using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Episodi visti delle serie TV. Gli episodi non sono nel DB: esistenza, data di uscita e durata
    // si controllano su TMDB (con la cache di TmdbService); qui si salva solo chi ha visto cosa.
    public class EpisodiService
    {
        private readonly CinemaContext _db;
        private readonly TmdbService _tmdb;

        public EpisodiService(CinemaContext db, TmdbService tmdb)
        {
            _db = db;
            _tmdb = tmdb;
        }

        // Id TMDB di una serie visibile nel catalogo; null se non è una serie collegata a TMDB
        public Task<int?> TmdbIdSerie(int idFilm) =>
            _db.Film
                .Where(f => f.IdFilm == idFilm && f.Stato == StatiFilm.Approvato && f.Tipo == TipiTitolo.Serie)
                .Select(f => f.TmdbId)
                .FirstOrDefaultAsync();

        // null se la serie non esiste (o non è su TMDB)
        public async Task<AvanzamentoSerieDto?> Avanzamento(int idFilm, int idUtente)
        {
            var tmdbId = await TmdbIdSerie(idFilm);
            if (tmdbId == null) return null;
            var struttura = await _tmdb.Struttura(tmdbId.Value);
            if (struttura == null) return null;

            var visti = await _db.EpisodiVisti
                .Where(v => v.IdUtente == idUtente && v.IdFilm == idFilm)
                .OrderBy(v => v.Stagione).ThenBy(v => v.Episodio)
                .Select(v => new EpisodioVistoDto { Stagione = v.Stagione, Episodio = v.Episodio, DataVisione = v.DataVisione })
                .ToListAsync();

            var giaVisti = visti.Select(v => (v.Stagione, v.Episodio)).ToHashSet();
            var numeroVisti = giaVisti.Count(v => struttura.Uscito(v.Stagione, v.Episodio));

            EpisodioRiferimentoDto? prossimo = null;
            if (ProssimoDaVedere(struttura, giaVisti) is { } p)
            {
                // Titolo e data dell'episodio: una sola stagione da TMDB (in cache)
                var episodio = (await _tmdb.Stagione(tmdbId.Value, p.Stagione))?.Episodi.FirstOrDefault(e => e.Numero == p.Episodio);
                prossimo = new EpisodioRiferimentoDto
                {
                    Stagione = p.Stagione,
                    Episodio = p.Episodio,
                    Titolo = episodio?.Titolo,
                    DataUscita = episodio?.DataUscita
                };
            }

            return new AvanzamentoSerieDto
            {
                IdFilm = idFilm,
                EpisodiVisti = numeroVisti,
                EpisodiUsciti = struttura.EpisodiUsciti,
                Stato = StatiSerie.Calcola(numeroVisti, struttura.EpisodiUsciti, struttura.Conclusa),
                Visti = visti,
                Prossimo = prossimo,
                InUscita = struttura.InUscita
            };
        }

        // Il prossimo episodio è quello dopo il più avanzato già visto (come si guarda di solito una serie);
        // se dopo non c'è niente, il primo rimasto indietro. null se si è in pari.
        private static (int Stagione, int Episodio)? ProssimoDaVedere(StrutturaSerie struttura, HashSet<(int, int)> giaVisti)
        {
            var usciti = struttura.EpisodiUscitiInOrdine().ToList();
            var ultimoVisto = usciti.FindLastIndex(giaVisti.Contains);
            foreach (var e in usciti.Skip(ultimoVisto + 1).Concat(usciti.Take(ultimoVisto + 1)))
                if (!giaVisti.Contains(e)) return e;
            return null;
        }

        // ---- Segnare e togliere ----

        // Un episodio. NonTrovato se la serie o l'episodio non esistono, NonUscito se non è ancora uscito.
        public async Task<EsitoEpisodi> Segna(int idFilm, int idUtente, int stagione, int episodio)
        {
            var tmdbId = await TmdbIdSerie(idFilm);
            if (tmdbId == null) return EsitoEpisodi.NonTrovato;
            var dettaglio = await _tmdb.Stagione(tmdbId.Value, stagione);
            var ep = dettaglio?.Episodi.FirstOrDefault(e => e.Numero == episodio);
            if (ep == null) return EsitoEpisodi.NonTrovato;
            if (!Uscito(ep)) return EsitoEpisodi.NonUscito;

            await Aggiungi(idUtente, idFilm, new[] { (stagione, ep) });
            return EsitoEpisodi.Ok;
        }

        // Tutti gli episodi già usciti di una stagione
        public async Task<EsitoEpisodi> SegnaStagione(int idFilm, int idUtente, int stagione)
        {
            var tmdbId = await TmdbIdSerie(idFilm);
            if (tmdbId == null) return EsitoEpisodi.NonTrovato;
            var dettaglio = await _tmdb.Stagione(tmdbId.Value, stagione);
            if (dettaglio == null) return EsitoEpisodi.NonTrovato;

            var usciti = dettaglio.Episodi.Where(Uscito).Select(e => (stagione, e)).ToList();
            if (usciti.Count == 0) return EsitoEpisodi.NonUscito;
            await Aggiungi(idUtente, idFilm, usciti);
            return EsitoEpisodi.Ok;
        }

        // L'episodio indicato e tutti quelli usciti prima (dalla stagione 1; gli speciali no)
        // "Ho visto tutta la serie": tutti gli episodi già usciti (fino all'ultimo uscito, speciali esclusi)
        public async Task<EsitoEpisodi> SegnaTutta(int idFilm, int idUtente)
        {
            var tmdbId = await TmdbIdSerie(idFilm);
            if (tmdbId == null) return EsitoEpisodi.NonTrovato;
            var struttura = await _tmdb.Struttura(tmdbId.Value);
            if (struttura == null) return EsitoEpisodi.NonTrovato;
            if (struttura.UltimoUscito is not { } ultimo) return EsitoEpisodi.NonUscito;
            return await SegnaFinoA(idFilm, idUtente, ultimo.Stagione, ultimo.Episodio);
        }

        public async Task<EsitoEpisodi> SegnaFinoA(int idFilm, int idUtente, int stagione, int episodio)
        {
            var tmdbId = await TmdbIdSerie(idFilm);
            if (tmdbId == null) return EsitoEpisodi.NonTrovato;
            var struttura = await _tmdb.Struttura(tmdbId.Value);
            if (struttura == null || !struttura.Stagioni.Any(s => s.Numero == stagione)) return EsitoEpisodi.NonTrovato;

            var daSegnare = new List<(int, EpisodioDto)>();
            foreach (var s in struttura.Stagioni.Where(s => s.Numero <= stagione))
            {
                var dettaglio = await _tmdb.Stagione(tmdbId.Value, s.Numero);
                if (dettaglio == null) continue;
                daSegnare.AddRange(dettaglio.Episodi
                    .Where(e => s.Numero < stagione || e.Numero <= episodio)
                    .Where(Uscito)
                    .Select(e => (s.Numero, e)));
            }

            if (!daSegnare.Any(x => x.Item1 == stagione && x.Item2.Numero == episodio))
                return daSegnare.Count == 0 ? EsitoEpisodi.NonTrovato : EsitoEpisodi.NonUscito;
            await Aggiungi(idUtente, idFilm, daSegnare);
            return EsitoEpisodi.Ok;
        }

        public Task Togli(int idFilm, int idUtente, int stagione, int episodio) =>
            _db.EpisodiVisti
                .Where(v => v.IdUtente == idUtente && v.IdFilm == idFilm && v.Stagione == stagione && v.Episodio == episodio)
                .ExecuteDeleteAsync();

        public Task TogliStagione(int idFilm, int idUtente, int stagione) =>
            _db.EpisodiVisti
                .Where(v => v.IdUtente == idUtente && v.IdFilm == idFilm && v.Stagione == stagione)
                .ExecuteDeleteAsync();

        // Salva solo gli episodi non ancora segnati. Come per i film, iniziare a guardare
        // una serie la toglie dalla lista "Da vedere".
        private async Task Aggiungi(int idUtente, int idFilm, IEnumerable<(int Stagione, EpisodioDto Episodio)> episodi)
        {
            var giaVisti = (await _db.EpisodiVisti
                    .Where(v => v.IdUtente == idUtente && v.IdFilm == idFilm)
                    .Select(v => new { v.Stagione, v.Episodio })
                    .ToListAsync())
                .Select(v => (v.Stagione, v.Episodio))
                .ToHashSet();

            var oggi = DateOnly.FromDateTime(DateTime.Now);
            var adesso = DateTime.Now;
            var nuovi = episodi
                .Where(e => !giaVisti.Contains((e.Stagione, e.Episodio.Numero)))
                .Select(e => new EpisodioVisto
                {
                    IdUtente = idUtente,
                    IdFilm = idFilm,
                    Stagione = e.Stagione,
                    Episodio = e.Episodio.Numero,
                    DataVisione = oggi,
                    DurataMinuti = e.Episodio.DurataMinuti,
                    DataCreazione = adesso
                })
                .ToList();

            using var transazione = await _db.Database.BeginTransactionAsync();
            if (nuovi.Count > 0)
            {
                _db.EpisodiVisti.AddRange(nuovi);
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Due richieste contemporanee sullo stesso episodio: il vincolo UNIQUE ha già fatto il suo lavoro
                    await transazione.RollbackAsync();
                    return;
                }
            }
            await _db.DaVedere.Where(d => d.IdUtente == idUtente && d.IdFilm == idFilm).ExecuteDeleteAsync();
            await transazione.CommitAsync();
        }

        private static bool Uscito(EpisodioDto e) =>
            e.DataUscita != null && string.CompareOrdinal(e.DataUscita, DateTime.Today.ToString("yyyy-MM-dd")) <= 0;

        // ---- Profilo ----

        // Le serie che l'utente ha iniziato, dalla visione più recente
        public async Task<List<SerieUtenteDto>> SerieUtente(int idUtente)
        {
            var visti = await _db.EpisodiVisti
                .Where(v => v.IdUtente == idUtente)
                .Select(v => new { v.IdFilm, v.Stagione, v.Episodio, v.DataVisione })
                .ToListAsync();
            if (visti.Count == 0) return new();

            var idSerie = visti.Select(v => v.IdFilm).Distinct().ToList();
            var serie = await _db.Film
                .Where(f => idSerie.Contains(f.IdFilm) && f.Stato == StatiFilm.Approvato && f.TmdbId != null)
                .Select(f => new { f.IdFilm, f.Titolo, f.ImmagineUrl, f.TmdbId })
                .ToListAsync();

            var risultato = new List<SerieUtenteDto>();
            foreach (var s in serie)
            {
                var struttura = await _tmdb.Struttura(s.TmdbId!.Value);
                if (struttura == null) continue;

                var suoi = visti.Where(v => v.IdFilm == s.IdFilm).ToList();
                var giaVisti = suoi.Select(v => (v.Stagione, v.Episodio)).ToHashSet();
                var numeroVisti = giaVisti.Count(v => struttura.Uscito(v.Item1, v.Item2));
                var prossimo = ProssimoDaVedere(struttura, giaVisti);

                risultato.Add(new SerieUtenteDto
                {
                    IdFilm = s.IdFilm,
                    Titolo = s.Titolo,
                    ImmagineUrl = s.ImmagineUrl,
                    EpisodiVisti = numeroVisti,
                    EpisodiUsciti = struttura.EpisodiUsciti,
                    Stato = StatiSerie.Calcola(numeroVisti, struttura.EpisodiUsciti, struttura.Conclusa),
                    ProssimaStagione = prossimo?.Stagione,
                    ProssimoEpisodio = prossimo?.Episodio,
                    UltimaVisione = suoi.Max(v => v.DataVisione)
                });
            }
            return risultato.OrderByDescending(r => r.UltimaVisione).ToList();
        }
    }

    public enum EsitoEpisodi { Ok, NonTrovato, NonUscito }
}
