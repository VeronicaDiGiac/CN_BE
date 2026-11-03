using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CinemaApi.Services
{
    // Unico punto che parla con TMDB (https://developer.themoviedb.org).
    // Il token resta sul server: il FE chiama solo le nostre API.
    // Il token va negli User Secrets: dotnet user-secrets set "Tmdb:Token" "<token di lettura v4>"
    public class TmdbService
    {
        private const string UrlImmagini = "https://image.tmdb.org/t/p/";
        private const string Lingua = "it-IT";
        // Paese per uscite e piattaforme di streaming
        private const string Regione = "IT";

        // Le risposte di TMDB sono in snake_case (poster_path, release_date…)
        private static readonly JsonSerializerOptions OpzioniJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        public TmdbService(HttpClient http, IMemoryCache cache, IConfiguration config)
        {
            _http = http;
            _cache = cache;
            _config = config;
        }

        // tipo: TipiTitolo.Film o TipiTitolo.Serie
        public Task<TmdbRicercaDto> Cerca(string testo, int pagina = 1, int? anno = null, string tipo = TipiTitolo.Film)
        {
            var serie = tipo == TipiTitolo.Serie;
            var url = $"search/{Percorso(tipo)}?query={Uri.EscapeDataString(testo)}&page={pagina}&language={Lingua}&include_adult=false";
            if (anno != null) url += serie ? $"&first_air_date_year={anno}" : $"&primary_release_year={anno}";
            return ElencoTmdb(url, $"tmdb:cerca:{tipo}:{testo.ToLowerInvariant()}:{pagina}:{anno}", TimeSpan.FromMinutes(10), tipo);
        }

        // Su TMDB i film stanno sotto /movie, le serie sotto /tv
        private static string Percorso(string tipo) => tipo == TipiTitolo.Serie ? "tv" : "movie";

        // Elenchi per il catalogo (Helpers.CategorieTmdb). null se la categoria non esiste.
        public Task<TmdbRicercaDto>? Elenco(string categoria, int pagina = 1) => categoria switch
        {
            CategorieTmdb.AlCinema => ElencoTmdb($"movie/now_playing?language={Lingua}&region={Regione}&page={pagina}",
                $"tmdb:alcinema:{pagina}", TimeSpan.FromHours(6), TipiTitolo.Film),
            CategorieTmdb.Tendenze => ElencoTmdb($"trending/movie/week?language={Lingua}&page={pagina}",
                $"tmdb:tendenze:{pagina}", TimeSpan.FromHours(6), TipiTitolo.Film),
            CategorieTmdb.Popolari => ElencoTmdb($"movie/popular?language={Lingua}&region={Regione}&page={pagina}",
                $"tmdb:popolari:{pagina}", TimeSpan.FromHours(6), TipiTitolo.Film),
            CategorieTmdb.InArrivo => UsciteInArrivo(pagina),
            CategorieTmdb.SerieTendenze => ElencoTmdb($"trending/tv/week?language={Lingua}&page={pagina}",
                $"tmdb:serie-tendenze:{pagina}", TimeSpan.FromHours(6), TipiTitolo.Serie),
            CategorieTmdb.SeriePopolari => ElencoTmdb($"tv/popular?language={Lingua}&page={pagina}",
                $"tmdb:serie-popolari:{pagina}", TimeSpan.FromHours(6), TipiTitolo.Serie),
            // Serie con un episodio in uscita nei prossimi 7 giorni
            CategorieTmdb.SerieInOnda => ElencoTmdb($"tv/on_the_air?language={Lingua}&timezone=Europe/Rome&page={pagina}",
                $"tmdb:serie-inonda:{pagina}", TimeSpan.FromHours(6), TipiTitolo.Serie),
            _ => null
        };

        // Prossime uscite nei cinema italiani, dalla più vicina.
        // TMDB include anche le riedizioni di film vecchi con la data originale (es. un film del 1968):
        // in "In arrivo" confonderebbero, quindi si tengono solo le prime uscite dall'ultima settimana in poi.
        private async Task<TmdbRicercaDto> UsciteInArrivo(int pagina)
        {
            var elenco = await ElencoTmdb($"movie/upcoming?language={Lingua}&region={Regione}&page={pagina}",
                $"tmdb:uscite:{pagina}", TimeSpan.FromHours(6), TipiTitolo.Film);
            var daQuando = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
            elenco.Risultati = elenco.Risultati
                .Where(r => r.DataUscita != null && string.CompareOrdinal(r.DataUscita, daQuando) >= 0)
                .OrderBy(r => r.DataUscita)
                .ToList();
            return elenco;
        }

        // Qualsiasi elenco TMDB di film o di serie (ricerca, uscite…) convertito nei nostri DTO, con cache.
        // Le serie hanno name / first_air_date al posto di title / release_date.
        private async Task<TmdbRicercaDto> ElencoTmdb(string url, string chiaveCache, TimeSpan durataCache, string tipo)
        {
            if (_cache.TryGetValue(chiaveCache, out TmdbRicercaDto? inCache) && inCache != null) return Copia(inCache);

            var risposta = await Get<RispostaRicerca>(url) ?? new RispostaRicerca();
            var risultato = new TmdbRicercaDto
            {
                Pagina = risposta.Page,
                TotalePagine = risposta.TotalPages,
                TotaleRisultati = risposta.TotalResults,
                Risultati = risposta.Results.Select(r => new TmdbRisultatoDto
                {
                    TmdbId = r.Id,
                    Tipo = tipo,
                    Titolo = r.Title ?? r.Name ?? r.OriginalTitle ?? r.OriginalName ?? "",
                    TitoloOriginale = r.OriginalTitle ?? r.OriginalName,
                    AnnoUscita = Anno(r.ReleaseDate ?? r.FirstAirDate),
                    DataUscita = string.IsNullOrEmpty(r.ReleaseDate ?? r.FirstAirDate) ? null : r.ReleaseDate ?? r.FirstAirDate,
                    LocandinaUrl = Immagine(r.PosterPath, "w342"),
                    Descrizione = r.Overview
                }).ToList()
            };

            _cache.Set(chiaveCache, risultato, durataCache);
            return Copia(risultato);
        }

        // Trailer e piattaforme italiane di un film o di una serie (dati "dove guardarlo" di JustWatch tramite TMDB)
        public async Task<FilmExtraDto> Extra(int tmdbId, string tipo = TipiTitolo.Film)
        {
            var chiave = $"tmdb:extra:{tipo}:{tmdbId}";
            if (_cache.TryGetValue(chiave, out FilmExtraDto? inCache) && inCache != null) return inCache;

            var film = await Get<RispostaExtra>(
                $"{Percorso(tipo)}/{tmdbId}?language={Lingua}&append_to_response=videos,watch/providers&include_video_language=it,en,null");
            var extra = new FilmExtraDto();
            if (film != null)
            {
                // Per le serie: durata tipica di un episodio
                var durata = film.Runtime ?? film.EpisodeRunTime?.FirstOrDefault();
                extra.DurataMinuti = durata > 0 ? durata : null;

                // Trailer ufficiale su YouTube, preferendo quello italiano
                extra.TrailerYoutube = film.Videos?.Results
                    .Where(v => v.Site == "YouTube" && v.Type == "Trailer" && !string.IsNullOrEmpty(v.Key))
                    .OrderBy(v => v.Iso6391 == "it" ? 0 : 1)
                    .ThenBy(v => v.Official ? 0 : 1)
                    .Select(v => v.Key)
                    .FirstOrDefault();

                if (film.WatchProviders?.Results.TryGetValue(Regione, out var italia) == true)
                {
                    extra.LinkDoveGuardarlo = italia.Link;
                    extra.Abbonamento = Piattaforme(italia.Flatrate);
                    extra.Noleggio = Piattaforme(italia.Rent);
                    extra.Acquisto = Piattaforme(italia.Buy);
                }
            }

            _cache.Set(chiave, extra, TimeSpan.FromHours(12));
            return extra;
        }

        private static List<PiattaformaDto> Piattaforme(List<Piattaforma>? piattaforme) =>
            (piattaforme ?? new())
                .OrderBy(p => p.DisplayPriority)
                .Select(p => new PiattaformaDto { Nome = p.ProviderName ?? "", LogoUrl = Immagine(p.LogoPath, "w92") })
                .ToList();

        // Il controller valorizza IdFilm sui risultati: si lavora su una copia,
        // così due richieste contemporanee non modificano lo stesso oggetto in cache
        private static TmdbRicercaDto Copia(TmdbRicercaDto r) => new()
        {
            Pagina = r.Pagina,
            TotalePagine = r.TotalePagine,
            TotaleRisultati = r.TotaleRisultati,
            Risultati = r.Risultati.Select(x => new TmdbRisultatoDto
            {
                TmdbId = x.TmdbId,
                Tipo = x.Tipo,
                Titolo = x.Titolo,
                TitoloOriginale = x.TitoloOriginale,
                AnnoUscita = x.AnnoUscita,
                DataUscita = x.DataUscita,
                LocandinaUrl = x.LocandinaUrl,
                Descrizione = x.Descrizione
            }).ToList()
        };

        // null se il film non esiste su TMDB
        public async Task<FilmTmdb?> Dettaglio(int tmdbId)
        {
            var chiave = $"tmdb:film:{tmdbId}";
            if (_cache.TryGetValue(chiave, out FilmTmdb? inCache) && inCache != null) return inCache;

            var film = await Get<RispostaFilm>($"movie/{tmdbId}?language={Lingua}&append_to_response=credits");
            if (film == null) return null;

            // Non tutti i film hanno la trama in italiano: in quel caso si usa quella inglese
            var trama = film.Overview;
            if (string.IsNullOrWhiteSpace(trama))
                trama = (await Get<RispostaFilm>($"movie/{tmdbId}?language=en-US"))?.Overview;

            var risultato = new FilmTmdb
            {
                TmdbId = film.Id,
                Titolo = film.Title ?? film.OriginalTitle ?? "",
                TitoloOriginale = film.OriginalTitle,
                AnnoUscita = Anno(film.ReleaseDate),
                Regista = Unisci(film.Credits?.Crew.Where(c => c.Job == "Director").Select(c => c.Name)),
                Genere = Unisci(film.Genres.Select(g => g.Name)),
                Attori = Unisci(film.Credits?.Cast.OrderBy(c => c.Order).Take(5).Select(c => c.Name)),
                LocandinaUrl = Immagine(film.PosterPath, "w500"),
                Descrizione = trama,
                DurataMinuti = film.Runtime > 0 ? film.Runtime : null
            };

            _cache.Set(chiave, risultato, TimeSpan.FromHours(12));
            return risultato;
        }

        // ---- Serie TV ----

        // Dati di una serie convertiti nei campi del catalogo; null se non esiste su TMDB.
        // "Regista" per le serie sono gli ideatori.
        public async Task<FilmTmdb?> DettaglioSerie(int tmdbId)
        {
            var serie = await GetSerie(tmdbId);
            if (serie == null) return null;

            var trama = serie.Overview;
            if (string.IsNullOrWhiteSpace(trama))
                trama = (await Get<RispostaSerie>($"tv/{tmdbId}?language=en-US"))?.Overview;

            var durata = serie.EpisodeRunTime.FirstOrDefault();
            return new FilmTmdb
            {
                Tipo = TipiTitolo.Serie,
                TmdbId = serie.Id,
                Titolo = serie.Name ?? serie.OriginalName ?? "",
                TitoloOriginale = serie.OriginalName,
                AnnoUscita = Anno(serie.FirstAirDate),
                Regista = Unisci(serie.CreatedBy.Select(c => c.Name)),
                Genere = Unisci(serie.Genres.Select(g => g.Name)),
                Attori = Unisci(serie.AggregateCredits?.Cast.OrderBy(c => c.Order).Take(5).Select(c => c.Name)),
                LocandinaUrl = Immagine(serie.PosterPath, "w500"),
                Descrizione = trama,
                DurataMinuti = durata > 0 ? durata : null,
                NumeroStagioni = serie.NumberOfSeasons > 0 ? serie.NumberOfSeasons : null
            };
        }

        // Stagioni di una serie, gli episodi speciali (stagione 0) in fondo; null se la serie non esiste
        public async Task<List<StagioneDto>?> Stagioni(int tmdbId)
        {
            var serie = await GetSerie(tmdbId);
            return serie?.Seasons
                .OrderBy(s => s.SeasonNumber == 0 ? int.MaxValue : s.SeasonNumber)
                .Select(s => new StagioneDto
                {
                    Numero = s.SeasonNumber,
                    Nome = s.Name ?? $"Stagione {s.SeasonNumber}",
                    NumeroEpisodi = s.EpisodeCount,
                    DataUscita = string.IsNullOrEmpty(s.AirDate) ? null : s.AirDate,
                    LocandinaUrl = Immagine(s.PosterPath, "w185")
                })
                .ToList();
        }

        // Episodi di una stagione; null se la serie o la stagione non esistono
        public async Task<StagioneDettaglioDto?> Stagione(int tmdbId, int numero)
        {
            var chiave = $"tmdb:stagione:{tmdbId}:{numero}";
            if (_cache.TryGetValue(chiave, out StagioneDettaglioDto? inCache) && inCache != null) return inCache;

            var stagione = await Get<RispostaStagione>($"tv/{tmdbId}/season/{numero}?language={Lingua}");
            if (stagione == null) return null;

            var risultato = new StagioneDettaglioDto
            {
                Numero = stagione.SeasonNumber,
                Nome = stagione.Name ?? $"Stagione {stagione.SeasonNumber}",
                Descrizione = string.IsNullOrWhiteSpace(stagione.Overview) ? null : stagione.Overview,
                Episodi = stagione.Episodes
                    .OrderBy(e => e.EpisodeNumber)
                    .Select(e => new EpisodioDto
                    {
                        Numero = e.EpisodeNumber,
                        Titolo = string.IsNullOrWhiteSpace(e.Name) ? $"Episodio {e.EpisodeNumber}" : e.Name,
                        DataUscita = string.IsNullOrEmpty(e.AirDate) ? null : e.AirDate,
                        DurataMinuti = e.Runtime > 0 ? e.Runtime : null,
                        Descrizione = string.IsNullOrWhiteSpace(e.Overview) ? null : e.Overview,
                        ImmagineUrl = Immagine(e.StillPath, "w300")
                    })
                    .ToList()
            };
            // Le stagioni in corso cambiano spesso (nuovi episodi, titoli, date)
            _cache.Set(chiave, risultato, TimeSpan.FromHours(6));
            return risultato;
        }

        // Risposta completa di TMDB per una serie, in cache: la usano dettaglio e stagioni
        private async Task<RispostaSerie?> GetSerie(int tmdbId)
        {
            var chiave = $"tmdb:serie:{tmdbId}";
            if (_cache.TryGetValue(chiave, out RispostaSerie? inCache) && inCache != null) return inCache;

            var serie = await Get<RispostaSerie>($"tv/{tmdbId}?language={Lingua}&append_to_response=aggregate_credits");
            if (serie != null) _cache.Set(chiave, serie, TimeSpan.FromHours(6));
            return serie;
        }

        // null se TMDB risponde 404
        private async Task<T?> Get<T>(string url) where T : class
        {
            var token = _config["Tmdb:Token"];
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException(
                    "Tmdb:Token mancante. Impostalo con: dotnet user-secrets set \"Tmdb:Token\" \"<token di lettura v4>\"");

            using var richiesta = new HttpRequestMessage(HttpMethod.Get, url);
            richiesta.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var risposta = await _http.SendAsync(richiesta);
            if (risposta.StatusCode == HttpStatusCode.NotFound) return null;
            risposta.EnsureSuccessStatusCode();

            await using var stream = await risposta.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, OpzioniJson);
        }

        private static int? Anno(string? data) =>
            data != null && data.Length >= 4 && int.TryParse(data[..4], out var anno) ? anno : null;

        private static string? Immagine(string? percorso, string dimensione) =>
            string.IsNullOrEmpty(percorso) ? null : $"{UrlImmagini}{dimensione}{percorso}";

        private static string? Unisci(IEnumerable<string?>? valori)
        {
            var testo = string.Join(", ", valori?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string?>());
            return testo == "" ? null : testo;
        }

        // ---- Forma delle risposte TMDB (solo i campi che usiamo) ----

        private class RispostaRicerca
        {
            public int Page { get; set; }
            public int TotalPages { get; set; }
            public int TotalResults { get; set; }
            public List<RisultatoRicerca> Results { get; set; } = new();
        }

        private class RisultatoRicerca
        {
            public int Id { get; set; }
            // Film
            public string? Title { get; set; }
            public string? OriginalTitle { get; set; }
            public string? ReleaseDate { get; set; }
            // Serie
            public string? Name { get; set; }
            public string? OriginalName { get; set; }
            public string? FirstAirDate { get; set; }

            public string? PosterPath { get; set; }
            public string? Overview { get; set; }
        }

        private class RispostaSerie : RisultatoRicerca
        {
            public List<Genere> Genres { get; set; } = new();
            public List<Persona> CreatedBy { get; set; } = new();
            public Crediti? AggregateCredits { get; set; }
            public List<int> EpisodeRunTime { get; set; } = new();
            public int NumberOfSeasons { get; set; }
            public List<StagioneTmdb> Seasons { get; set; } = new();
        }

        private class StagioneTmdb
        {
            public int SeasonNumber { get; set; }
            public string? Name { get; set; }
            public int EpisodeCount { get; set; }
            public string? AirDate { get; set; }
            public string? PosterPath { get; set; }
        }

        private class RispostaStagione
        {
            public int SeasonNumber { get; set; }
            public string? Name { get; set; }
            public string? Overview { get; set; }
            public List<EpisodioTmdb> Episodes { get; set; } = new();
        }

        private class EpisodioTmdb
        {
            public int EpisodeNumber { get; set; }
            public string? Name { get; set; }
            public string? AirDate { get; set; }
            public int? Runtime { get; set; }
            public string? Overview { get; set; }
            public string? StillPath { get; set; }
        }

        private class RispostaFilm : RisultatoRicerca
        {
            public List<Genere> Genres { get; set; } = new();
            public Crediti? Credits { get; set; }
            // Minuti; 0 o assente se TMDB non lo conosce
            public int? Runtime { get; set; }
        }

        private class Genere { public string? Name { get; set; } }

        private class RispostaExtra
        {
            public int? Runtime { get; set; }
            // Solo serie: durate tipiche degli episodi
            public List<int>? EpisodeRunTime { get; set; }
            public ElencoVideo? Videos { get; set; }
            // "watch/providers" non si converte in snake_case: nome esplicito
            [JsonPropertyName("watch/providers")]
            public ElencoPiattaforme? WatchProviders { get; set; }
        }

        private class ElencoVideo { public List<Video> Results { get; set; } = new(); }

        private class Video
        {
            public string? Key { get; set; }
            public string? Site { get; set; }
            public string? Type { get; set; }
            public bool Official { get; set; }
            [JsonPropertyName("iso_639_1")]
            public string? Iso6391 { get; set; }
        }

        // Risultati per paese: "IT" -> piattaforme italiane
        private class ElencoPiattaforme { public Dictionary<string, PiattaformePaese> Results { get; set; } = new(); }

        private class PiattaformePaese
        {
            public string? Link { get; set; }
            public List<Piattaforma>? Flatrate { get; set; }
            public List<Piattaforma>? Rent { get; set; }
            public List<Piattaforma>? Buy { get; set; }
        }

        private class Piattaforma
        {
            public string? ProviderName { get; set; }
            public string? LogoPath { get; set; }
            public int DisplayPriority { get; set; }
        }

        private class Crediti
        {
            public List<Persona> Cast { get; set; } = new();
            public List<Persona> Crew { get; set; } = new();
        }

        private class Persona
        {
            public string? Name { get; set; }
            public string? Job { get; set; }
            public int Order { get; set; }
        }
    }

    // Dati di un film o di una serie TMDB già convertiti nei campi del nostro catalogo
    public class FilmTmdb
    {
        public string Tipo { get; set; } = TipiTitolo.Film;
        public int? NumeroStagioni { get; set; }
        public int TmdbId { get; set; }
        public string Titolo { get; set; } = "";
        public string? TitoloOriginale { get; set; }
        public int? AnnoUscita { get; set; }
        public string? Regista { get; set; }
        public string? Genere { get; set; }
        public string? Attori { get; set; }
        public string? LocandinaUrl { get; set; }
        public string? Descrizione { get; set; }
        public int? DurataMinuti { get; set; }
    }
}
