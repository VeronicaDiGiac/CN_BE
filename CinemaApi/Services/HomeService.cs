using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CinemaApi.Services
{
    // Sezioni della home: "Continua a guardare", "Da vedere, ora in streaming", classifica di Cineverse,
    // curiosità del giorno e discussioni più animate. Sondaggio e film club hanno i loro servizi.
    public class HomeService
    {
        private const int MassimoContinua = 10;
        private const int MassimoStreaming = 12;
        // Titoli della lista "Da vedere" controllati su TMDB (i più recenti): ognuno è una richiesta
        private const int DaVedereControllati = 24;
        private const int MassimoClassifica = 10;
        private const int MassimoDiscussioni = 6;
        private const int GiorniDiscussioni = 7;
        private const int LunghezzaAnteprima = 140;

        private readonly CinemaContext _db;
        private readonly TmdbService _tmdb;
        private readonly EpisodiService _episodi;
        private readonly WikipediaService _wikipedia;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HomeService> _log;

        public HomeService(CinemaContext db, TmdbService tmdb, EpisodiService episodi, WikipediaService wikipedia,
            IMemoryCache cache, ILogger<HomeService> log)
        {
            _db = db;
            _tmdb = tmdb;
            _episodi = episodi;
            _wikipedia = wikipedia;
            _cache = cache;
            _log = log;
        }

        // Serie iniziate e non finite, dalla visione più recente, con il prossimo episodio da vedere
        public async Task<List<ContinuaGuardareDto>> ContinuaAGuardare(int idUtente)
        {
            var serie = (await _episodi.SerieUtente(idUtente))
                .Where(s => s.Stato == StatiSerie.InCorso && s.ProssimaStagione != null && s.ProssimoEpisodio != null)
                .Take(MassimoContinua)
                .ToList();
            if (serie.Count == 0) return new();

            var ids = serie.Select(s => s.IdFilm).ToList();
            var tmdbIds = await _db.Film.Where(f => ids.Contains(f.IdFilm) && f.TmdbId != null)
                .ToDictionaryAsync(f => f.IdFilm, f => f.TmdbId!.Value);

            var risultato = new List<ContinuaGuardareDto>();
            foreach (var s in serie)
            {
                var voce = new ContinuaGuardareDto
                {
                    IdFilm = s.IdFilm,
                    Titolo = s.Titolo,
                    ImmagineUrl = s.ImmagineUrl,
                    Stagione = s.ProssimaStagione!.Value,
                    Episodio = s.ProssimoEpisodio!.Value,
                    EpisodiVisti = s.EpisodiVisti,
                    EpisodiUsciti = s.EpisodiUsciti
                };
                // Titolo e immagine dell'episodio: facoltativi (la stagione è in cache)
                if (tmdbIds.TryGetValue(s.IdFilm, out var tmdbId))
                {
                    try
                    {
                        var episodio = (await _tmdb.Stagione(tmdbId, voce.Stagione))?.Episodi.FirstOrDefault(e => e.Numero == voce.Episodio);
                        voce.TitoloEpisodio = string.IsNullOrWhiteSpace(episodio?.Titolo) ? null : episodio.Titolo;
                        voce.ImmagineEpisodio = episodio?.ImmagineUrl;
                    }
                    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                    {
                        _log.LogWarning(ex, "Episodio non letto da TMDB per la serie {IdFilm}", s.IdFilm);
                    }
                }
                risultato.Add(voce);
            }
            return risultato;
        }

        // Titoli della lista "Da vedere" disponibili in abbonamento in Italia (dati JustWatch tramite TMDB, in cache)
        public async Task<List<DaVedereInStreamingDto>> DaVedereInStreaming(int idUtente)
        {
            var titoli = await _db.DaVedere
                .Where(d => d.IdUtente == idUtente && d.Film!.Stato == StatiFilm.Approvato && d.Film.TmdbId != null)
                .OrderByDescending(d => d.DataAggiunta)
                .Take(DaVedereControllati)
                .Select(d => new { d.IdFilm, d.Film!.Titolo, d.Film.ImmagineUrl, d.Film.Tipo, TmdbId = d.Film.TmdbId!.Value })
                .ToListAsync();

            // Poche richieste alla volta, per non farsi limitare da TMDB
            using var limite = new SemaphoreSlim(4);
            var controlli = titoli.Select(async t =>
            {
                await limite.WaitAsync();
                try
                {
                    var extra = await _tmdb.Extra(t.TmdbId, t.Tipo);
                    return extra.Abbonamento.Count == 0 ? null : new DaVedereInStreamingDto
                    {
                        IdFilm = t.IdFilm,
                        Titolo = t.Titolo,
                        ImmagineUrl = t.ImmagineUrl,
                        Tipo = t.Tipo,
                        Piattaforme = extra.Abbonamento
                    };
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    _log.LogWarning(ex, "Piattaforme non lette da TMDB per {IdFilm}", t.IdFilm);
                    return null;
                }
                finally { limite.Release(); }
            });
            var esiti = await Task.WhenAll(controlli);
            return esiti.Where(e => e != null).Take(MassimoStreaming).ToList()!;
        }

        // I più visti (utenti diversi che l'hanno segnato visto, o hanno visto un episodio) e i meglio
        // votati negli ultimi giorni. Meglio votati: almeno 2 voti nel periodo, se ce ne sono abbastanza
        public async Task<ClassificaDto> Classifica(int giorni)
        {
            var chiave = $"home:classifica:{giorni}";
            if (_cache.TryGetValue(chiave, out ClassificaDto? inCache) && inCache != null) return inCache;

            var dal = DateTime.Now.AddDays(-giorni);
            var spettatori = _db.Visioni.Where(v => v.DataCreazione >= dal).Select(v => new { v.IdUtente, v.IdFilm })
                .Union(_db.EpisodiVisti.Where(e => e.DataCreazione >= dal).Select(e => new { e.IdUtente, e.IdFilm }));
            var piuVisti = await spettatori
                .GroupBy(x => x.IdFilm)
                .Select(g => new { IdFilm = g.Key, Spettatori = g.Count() })
                .Join(_db.Film.Where(f => f.Stato == StatiFilm.Approvato), x => x.IdFilm, f => f.IdFilm, (x, f) => new TitoloClassificaDto
                {
                    IdFilm = f.IdFilm,
                    Titolo = f.Titolo,
                    ImmagineUrl = f.ImmagineUrl,
                    AnnoUscita = f.AnnoUscita,
                    Tipo = f.Tipo,
                    Spettatori = x.Spettatori
                })
                .OrderByDescending(v => v.Spettatori).ThenBy(v => v.Titolo)
                .Take(MassimoClassifica)
                .ToListAsync();

            var voti = _db.VotiFilm.Where(v => v.DataVoto >= dal)
                .GroupBy(v => v.IdFilm)
                .Select(g => new { IdFilm = g.Key, Media = g.Average(v => (double)v.Voto), Numero = g.Count() });
            var minimo = await voti.CountAsync(v => v.Numero >= 2) >= 3 ? 2 : 1;
            var meglioVotati = await voti.Where(v => v.Numero >= minimo)
                .Join(_db.Film.Where(f => f.Stato == StatiFilm.Approvato), x => x.IdFilm, f => f.IdFilm, (x, f) => new TitoloClassificaDto
                {
                    IdFilm = f.IdFilm,
                    Titolo = f.Titolo,
                    ImmagineUrl = f.ImmagineUrl,
                    AnnoUscita = f.AnnoUscita,
                    Tipo = f.Tipo,
                    VotoMedio = x.Media,
                    NumeroVoti = x.Numero
                })
                .OrderByDescending(v => v.VotoMedio).ThenByDescending(v => v.NumeroVoti).ThenBy(v => v.Titolo)
                .Take(MassimoClassifica)
                .ToListAsync();

            var risultato = new ClassificaDto { Giorni = giorni, PiuVisti = piuVisti, MeglioVotati = meglioVotati };
            _cache.Set(chiave, risultato, TimeSpan.FromMinutes(10));
            return risultato;
        }

        // Ogni giorno un titolo del catalogo (lo stesso per tutti) e una sezione della sua pagina Wikipedia.
        // Si provano alcuni titoli finché uno ha delle curiosità; null se nessuno ne ha.
        public async Task<CuriositaGiornoDto?> CuriositaDelGiorno()
        {
            var oggi = DateOnly.FromDateTime(DateTime.Now);
            var chiave = $"home:curiosita:{oggi:yyyy-MM-dd}";
            if (_cache.TryGetValue(chiave, out CuriositaGiornoDto? inCache)) return inCache;

            var candidati = await _db.Film
                .Where(f => f.Stato == StatiFilm.Approvato && f.TmdbId != null)
                .OrderBy(f => f.IdFilm)
                .Select(f => new { f.IdFilm, f.Titolo, f.ImmagineUrl, f.AnnoUscita })
                .ToListAsync();
            if (candidati.Count == 0) return null;

            CuriositaGiornoDto? risultato = null;
            var giorno = oggi.DayNumber;
            var tentativi = Math.Min(10, candidati.Count);
            for (var i = 0; i < tentativi && risultato == null; i++)
            {
                // Passo primo rispetto a quasi ogni numero di titoli: giorni vicini, titoli lontani
                var film = candidati[(int)(((long)giorno * 7919 + i * 104729L) % candidati.Count)];
                CuriositaDto? curiosita = null;
                try { curiosita = await _wikipedia.Curiosita(film.IdFilm); }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    _log.LogWarning(ex, "Curiosità non lette da Wikipedia per {IdFilm}", film.IdFilm);
                }
                if (curiosita == null || curiosita.Sezioni.Count == 0) continue;

                var sezione = curiosita.Sezioni[giorno % curiosita.Sezioni.Count];
                risultato = new CuriositaGiornoDto
                {
                    IdFilm = film.IdFilm,
                    Titolo = film.Titolo,
                    ImmagineUrl = film.ImmagineUrl,
                    AnnoUscita = film.AnnoUscita,
                    Lingua = curiosita.Lingua,
                    TitoloSezione = sezione.Titolo,
                    Testo = sezione.Testo,
                    Url = curiosita.Url,
                    TitoloPagina = curiosita.TitoloPagina
                };
            }

            // Se non si è trovato niente si riprova tra un po', non domani
            _cache.Set(chiave, risultato, risultato == null ? TimeSpan.FromMinutes(30) : TimeSpan.FromHours(24));
            return risultato;
        }

        // Le discussioni con più messaggi nell'ultima settimana; se sono poche, si aggiungono
        // quelle con l'attività più recente. Niente fili degli episodi.
        public async Task<List<DiscussioneAnimataDto>> DiscussioniAnimate()
        {
            var dal = DateTime.Now.AddDays(-GiorniDiscussioni);
            var visibili = _db.Discussioni.Where(d => !d.FiloEpisodio && d.Film!.Stato == StatiFilm.Approvato);

            var recenti = await _db.Commenti
                .Where(c => c.IdDiscussione != null && c.DataCommento >= dal)
                .GroupBy(c => c.IdDiscussione!.Value)
                .Select(g => new { IdDiscussione = g.Key, Numero = g.Count() })
                .Join(visibili, x => x.IdDiscussione, d => d.IdDiscussione, (x, d) => new { x.IdDiscussione, x.Numero, d.DataUltimaAttivita })
                .OrderByDescending(x => x.Numero).ThenByDescending(x => x.DataUltimaAttivita)
                .Take(MassimoDiscussioni)
                .ToListAsync();

            var ids = recenti.Select(r => r.IdDiscussione).ToList();
            if (ids.Count < MassimoDiscussioni)
            {
                ids.AddRange(await visibili
                    .Where(d => !ids.Contains(d.IdDiscussione))
                    .OrderByDescending(d => d.DataUltimaAttivita)
                    .Select(d => d.IdDiscussione)
                    .Take(MassimoDiscussioni - ids.Count)
                    .ToListAsync());
            }
            if (ids.Count == 0) return new();

            var discussioni = await _db.Discussioni
                .Where(d => ids.Contains(d.IdDiscussione))
                .Select(d => new DiscussioneAnimataDto
                {
                    IdDiscussione = d.IdDiscussione,
                    IdFilm = d.IdFilm,
                    TitoloFilm = d.Film!.Titolo,
                    ImmagineUrl = d.Film.ImmagineUrl,
                    Titolo = d.Titolo,
                    Spoiler = d.Spoiler,
                    MessaggiTotali = d.Commenti!.Count(),
                    DataUltimaAttivita = d.DataUltimaAttivita
                })
                .ToListAsync();

            var ultimi = await _db.Commenti
                .Where(c => c.IdDiscussione != null && ids.Contains(c.IdDiscussione.Value))
                .GroupBy(c => c.IdDiscussione!.Value)
                .Select(g => g.OrderByDescending(c => c.DataCommento).ThenByDescending(c => c.IdCommento)
                    .Select(c => new { IdDiscussione = c.IdDiscussione!.Value, c.IdUtente, c.Utente!.UserName, c.Contenuto, c.Spoiler, c.DataCommento })
                    .First())
                .ToListAsync();

            foreach (var d in discussioni)
            {
                d.MessaggiRecenti = recenti.FirstOrDefault(r => r.IdDiscussione == d.IdDiscussione)?.Numero ?? 0;
                var ultimo = ultimi.FirstOrDefault(u => u.IdDiscussione == d.IdDiscussione);
                if (ultimo == null) continue;
                var nascosto = ultimo.Spoiler || d.Spoiler;
                d.UltimoMessaggio = new UltimoMessaggioDto
                {
                    IdUtente = ultimo.IdUtente,
                    UserName = ultimo.UserName,
                    Spoiler = nascosto,
                    Anteprima = nascosto ? null : Anteprima(ultimo.Contenuto),
                    Data = ultimo.DataCommento
                };
            }
            // Stesso ordine della scelta: prima le più animate
            return ids.Select(id => discussioni.FirstOrDefault(d => d.IdDiscussione == id)).Where(d => d != null).ToList()!;
        }

        private static string? Anteprima(string? testo)
        {
            if (string.IsNullOrWhiteSpace(testo)) return null;
            var pulito = string.Join(' ', testo.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (pulito.Length <= LunghezzaAnteprima) return pulito;
            var taglio = pulito.LastIndexOf(' ', LunghezzaAnteprima);
            return pulito[..(taglio > 60 ? taglio : LunghezzaAnteprima)].TrimEnd(',', '.', ';', ':') + "…";
        }
    }
}
