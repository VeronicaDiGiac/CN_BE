using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CinemaApi.Services
{
    // Puntate in arrivo e appena uscite delle serie che un utente segue:
    // il calendario e la notifica "nuovo episodio". Le date vengono da TMDB (in cache).
    public class CalendarioSerieService
    {
        // Il calendario mostra anche l'ultima settimana, per chi è rimasto indietro di qualche giorno
        public const int GiorniPassatiCalendario = 7;
        public const int GiorniFuturiCalendario = 90;
        // Un episodio uscito da più di così non è più "nuovo": non si notifica
        public const int GiorniNovitaEpisodio = 3;

        // Controllo dei nuovi episodi per un singolo utente (quando usa l'app): al massimo ogni 3 ore
        private static readonly TimeSpan IntervalloControlloUtente = TimeSpan.FromHours(3);

        private readonly CinemaContext _db;
        private readonly TmdbService _tmdb;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CalendarioSerieService> _logger;

        public CalendarioSerieService(CinemaContext db, TmdbService tmdb, IMemoryCache cache, ILogger<CalendarioSerieService> logger)
        {
            _db = db;
            _tmdb = tmdb;
            _cache = cache;
            _logger = logger;
        }

        // ---- Avvisi delle nuove puntate ("Segui la serie") ----

        // true se l'utente riceve gli avvisi: la sua scelta, se l'ha fatta, altrimenti se ha visto
        // almeno un episodio. null se il film non è una serie del catalogo
        public async Task<bool?> Avvisi(int idUtente, int idFilm)
        {
            if (!await SerieValida(idFilm)) return null;
            var scelta = await _db.SerieSeguite
                .Where(s => s.IdUtente == idUtente && s.IdFilm == idFilm)
                .Select(s => (bool?)s.Avvisi)
                .FirstOrDefaultAsync();
            return scelta ?? await _db.EpisodiVisti.AnyAsync(v => v.IdUtente == idUtente && v.IdFilm == idFilm);
        }

        // Salva la scelta (attiva o disattiva gli avvisi). false se il film non è una serie del catalogo.
        // Attivandoli si controlla subito se c'è un episodio appena uscito.
        public async Task<bool> ImpostaAvvisi(int idUtente, int idFilm, bool avvisi)
        {
            if (!await SerieValida(idFilm)) return false;
            var riga = await _db.SerieSeguite.FindAsync(idUtente, idFilm);
            if (riga == null)
                _db.SerieSeguite.Add(new SerieSeguita { IdUtente = idUtente, IdFilm = idFilm, Avvisi = avvisi, DataModifica = DateTime.Now });
            else
            {
                riga.Avvisi = avvisi;
                riga.DataModifica = DateTime.Now;
            }
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Due richieste contemporanee: la chiave primaria ha già tenuto una sola riga
                _db.ChangeTracker.Clear();
            }

            if (avvisi)
            {
                try { await NotificaNuoviEpisodi(idUtente); }
                catch (Exception ex) { _logger.LogWarning(ex, "Controllo dei nuovi episodi dopo \"Segui la serie\" non riuscito"); }
            }
            return true;
        }

        // Online l'app può restare spenta per ore (IIS la ferma quando nessuno la usa) e con lei il
        // controllo periodico di NuoviEpisodiWorker: per questo il controllo parte anche quando
        // l'utente usa l'app (campanella). Al massimo ogni 3 ore per utente; un errore non blocca niente.
        public async Task ControllaSeServe(int idUtente)
        {
            var chiave = $"nuovi-episodi-{idUtente}";
            if (_cache.TryGetValue(chiave, out _)) return;
            _cache.Set(chiave, true, IntervalloControlloUtente);
            try
            {
                await NotificaNuoviEpisodi(idUtente);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Controllo dei nuovi episodi per l'utente {IdUtente} non riuscito", idUtente);
            }
        }

        private Task<bool> SerieValida(int idFilm) =>
            _db.Film.AnyAsync(f => f.IdFilm == idFilm && f.Tipo == TipiTitolo.Serie && f.Stato == StatiFilm.Approvato);

        // Serie seguite = iniziate (almeno un episodio visto) oppure nella lista "Da vedere"
        public async Task<List<PuntataCalendarioDto>> Calendario(int idUtente)
        {
            var iniziate = _db.EpisodiVisti.Where(v => v.IdUtente == idUtente).Select(v => v.IdFilm);
            var daVedere = _db.DaVedere.Where(d => d.IdUtente == idUtente).Select(d => d.IdFilm);
            var serie = await _db.Film
                .Where(f => f.Tipo == TipiTitolo.Serie && f.Stato == StatiFilm.Approvato && f.TmdbId != null)
                .Where(f => iniziate.Contains(f.IdFilm) || daVedere.Contains(f.IdFilm))
                .Select(f => new { f.IdFilm, f.Titolo, f.ImmagineUrl, f.TmdbId })
                .ToListAsync();

            var visti = (await _db.EpisodiVisti
                    .Where(v => v.IdUtente == idUtente)
                    .Select(v => new { v.IdFilm, v.Stagione, v.Episodio })
                    .ToListAsync())
                .Select(v => (v.IdFilm, v.Stagione, v.Episodio))
                .ToHashSet();

            var dal = Data(DateTime.Today.AddDays(-GiorniPassatiCalendario));
            var al = Data(DateTime.Today.AddDays(GiorniFuturiCalendario));
            var puntate = new List<PuntataCalendarioDto>();
            foreach (var s in serie)
            {
                foreach (var (stagione, ep) in await EpisodiTra(s.TmdbId!.Value, dal, al))
                {
                    puntate.Add(new PuntataCalendarioDto
                    {
                        IdFilm = s.IdFilm,
                        Titolo = s.Titolo,
                        ImmagineUrl = s.ImmagineUrl,
                        Stagione = stagione,
                        Episodio = ep.Numero,
                        TitoloEpisodio = ep.Titolo,
                        DataUscita = ep.DataUscita!,
                        Visto = visti.Contains((s.IdFilm, stagione, ep.Numero))
                    });
                }
            }
            return puntate.OrderBy(p => p.DataUscita).ThenBy(p => p.Titolo).ThenBy(p => p.Stagione).ThenBy(p => p.Episodio).ToList();
        }

        // Crea la notifica "nuovo episodio" per gli episodi usciti negli ultimi giorni, a chi la vuole
        // e non l'ha già visto. La vuole chi ha visto almeno un episodio (se non ha spento gli avvisi)
        // e chi segue la serie con "Segui la serie". Idempotente: ogni episodio si notifica una volta
        // sola per utente. soloUtente: controllo per un utente solo. Restituisce quante notifiche ha creato.
        public async Task<int> NotificaNuoviEpisodi(int? soloUtente = null)
        {
            var dagliEpisodi = _db.EpisodiVisti
                .Where(v => soloUtente == null || v.IdUtente == soloUtente)
                .Where(v => !_db.SerieSeguite.Any(s => s.IdUtente == v.IdUtente && s.IdFilm == v.IdFilm && !s.Avvisi))
                .Select(v => new { v.IdFilm, v.IdUtente });
            var seguite = _db.SerieSeguite
                .Where(s => s.Avvisi && (soloUtente == null || s.IdUtente == soloUtente))
                .Select(s => new { s.IdFilm, s.IdUtente });
            var spettatori = await dagliEpisodi.Union(seguite)
                .Join(_db.Film.Where(f => f.Tipo == TipiTitolo.Serie && f.Stato == StatiFilm.Approvato && f.TmdbId != null),
                      c => c.IdFilm, f => f.IdFilm, (c, f) => new { c.IdFilm, c.IdUtente, f.TmdbId })
                .ToListAsync();
            if (spettatori.Count == 0) return 0;

            var dal = Data(DateTime.Today.AddDays(-GiorniNovitaEpisodio));
            var oggi = Data(DateTime.Today);
            var create = 0;

            foreach (var serie in spettatori.GroupBy(s => new { s.IdFilm, s.TmdbId }))
            {
                var nuovi = await EpisodiTra(serie.Key.TmdbId!.Value, dal, oggi);
                if (nuovi.Count == 0) continue;

                var utenti = serie.Select(s => s.IdUtente).ToList();
                // Episodi già visti e notifiche già mandate per questa serie
                var giaVisti = (await _db.EpisodiVisti
                        .Where(v => v.IdFilm == serie.Key.IdFilm && utenti.Contains(v.IdUtente))
                        .Select(v => new { v.IdUtente, v.Stagione, v.Episodio })
                        .ToListAsync())
                    .Select(v => (v.IdUtente, v.Stagione, v.Episodio))
                    .ToHashSet();
                var giaNotificati = (await _db.Notifiche
                        .Where(n => n.Tipo == TipiNotifica.NuovoEpisodio && n.IdFilm == serie.Key.IdFilm && utenti.Contains(n.IdDestinatario))
                        .Select(n => new { n.IdDestinatario, n.Stagione, n.Episodio })
                        .ToListAsync())
                    .Select(n => (n.IdDestinatario, n.Stagione ?? 0, n.Episodio ?? 0))
                    .ToHashSet();

                foreach (var idUtente in utenti)
                {
                    foreach (var (stagione, ep) in nuovi)
                    {
                        var chiave = (idUtente, stagione, ep.Numero);
                        if (giaVisti.Contains(chiave) || giaNotificati.Contains(chiave)) continue;
                        _db.Notifiche.Add(new Notifica
                        {
                            IdDestinatario = idUtente,
                            IdMittente = null,
                            Tipo = TipiNotifica.NuovoEpisodio,
                            IdFilm = serie.Key.IdFilm,
                            Stagione = stagione,
                            Episodio = ep.Numero,
                            DataCreazione = DateTime.Now
                        });
                        create++;
                    }
                }
            }

            if (create == 0) return 0;
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Un altro controllo (periodico o di un altro utente) ha appena creato le stesse notifiche:
                // l'indice UNIQUE ha evitato i doppioni, al prossimo giro non ci sarà niente da fare
                _db.ChangeTracker.Clear();
                return 0;
            }
            return create;
        }

        // Episodi con data di uscita tra dal e al (compresi). Bastano le stagioni dell'ultimo
        // episodio uscito e del prossimo annunciato: le altre sono tutte nel passato o lontane.
        private async Task<List<(int Stagione, EpisodioDto Episodio)>> EpisodiTra(int tmdbId, string dal, string al)
        {
            var struttura = await _tmdb.Struttura(tmdbId);
            if (struttura == null) return new();

            var stagioni = new[] { struttura.UltimoUscito?.Stagione, struttura.InUscita?.Stagione }
                .Where(s => s is > 0)
                .Select(s => s!.Value)
                .Distinct();

            var risultato = new List<(int, EpisodioDto)>();
            foreach (var numero in stagioni)
            {
                var stagione = await _tmdb.Stagione(tmdbId, numero);
                if (stagione == null) continue;
                risultato.AddRange(stagione.Episodi
                    .Where(e => e.DataUscita != null
                                && string.CompareOrdinal(e.DataUscita, dal) >= 0
                                && string.CompareOrdinal(e.DataUscita, al) <= 0)
                    .Select(e => (numero, e)));
            }
            return risultato;
        }

        private static string Data(DateTime giorno) => giorno.ToString("yyyy-MM-dd");
    }
}
