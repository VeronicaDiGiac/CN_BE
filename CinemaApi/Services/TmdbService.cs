using CinemaApi.DTOs.ResponseDto;
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

        public Task<TmdbRicercaDto> Cerca(string testo, int pagina = 1, int? anno = null)
        {
            var url = $"search/movie?query={Uri.EscapeDataString(testo)}&page={pagina}&language={Lingua}&include_adult=false";
            if (anno != null) url += $"&primary_release_year={anno}";
            return ElencoFilm(url, $"tmdb:cerca:{testo.ToLowerInvariant()}:{pagina}:{anno}", TimeSpan.FromMinutes(10));
        }

        // Prossime uscite nei cinema italiani, dalla più vicina.
        // TMDB include anche le riedizioni di film vecchi con la data originale (es. un film del 1968):
        // in "In arrivo" confonderebbero, quindi si tengono solo le prime uscite dall'ultima settimana in poi.
        public async Task<TmdbRicercaDto> UsciteInArrivo(int pagina = 1)
        {
            var elenco = await ElencoFilm($"movie/upcoming?language={Lingua}&region={Regione}&page={pagina}",
                $"tmdb:uscite:{pagina}", TimeSpan.FromHours(6));
            var daQuando = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
            elenco.Risultati = elenco.Risultati
                .Where(r => r.DataUscita != null && string.CompareOrdinal(r.DataUscita, daQuando) >= 0)
                .OrderBy(r => r.DataUscita)
                .ToList();
            return elenco;
        }

        // Qualsiasi elenco di film TMDB (ricerca, uscite…) convertito nei nostri DTO, con cache
        private async Task<TmdbRicercaDto> ElencoFilm(string url, string chiaveCache, TimeSpan durataCache)
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
                    Titolo = r.Title ?? r.OriginalTitle ?? "",
                    TitoloOriginale = r.OriginalTitle,
                    AnnoUscita = Anno(r.ReleaseDate),
                    DataUscita = string.IsNullOrEmpty(r.ReleaseDate) ? null : r.ReleaseDate,
                    LocandinaUrl = Immagine(r.PosterPath, "w342"),
                    Descrizione = r.Overview
                }).ToList()
            };

            _cache.Set(chiaveCache, risultato, durataCache);
            return Copia(risultato);
        }

        // Trailer e piattaforme italiane di un film (dati "dove guardarlo" di JustWatch tramite TMDB)
        public async Task<FilmExtraDto> Extra(int tmdbId)
        {
            var chiave = $"tmdb:extra:{tmdbId}";
            if (_cache.TryGetValue(chiave, out FilmExtraDto? inCache) && inCache != null) return inCache;

            var film = await Get<RispostaExtra>(
                $"movie/{tmdbId}?language={Lingua}&append_to_response=videos,watch/providers&include_video_language=it,en,null");
            var extra = new FilmExtraDto();
            if (film != null)
            {
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
                Descrizione = trama
            };

            _cache.Set(chiave, risultato, TimeSpan.FromHours(12));
            return risultato;
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
            public string? Title { get; set; }
            public string? OriginalTitle { get; set; }
            public string? ReleaseDate { get; set; }
            public string? PosterPath { get; set; }
            public string? Overview { get; set; }
        }

        private class RispostaFilm : RisultatoRicerca
        {
            public List<Genere> Genres { get; set; } = new();
            public Crediti? Credits { get; set; }
        }

        private class Genere { public string? Name { get; set; } }

        private class RispostaExtra
        {
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

    // Dati di un film TMDB già convertiti nei campi del nostro catalogo
    public class FilmTmdb
    {
        public int TmdbId { get; set; }
        public string Titolo { get; set; } = "";
        public string? TitoloOriginale { get; set; }
        public int? AnnoUscita { get; set; }
        public string? Regista { get; set; }
        public string? Genere { get; set; }
        public string? Attori { get; set; }
        public string? LocandinaUrl { get; set; }
        public string? Descrizione { get; set; }
    }
}
