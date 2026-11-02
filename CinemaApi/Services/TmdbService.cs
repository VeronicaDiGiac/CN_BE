using CinemaApi.DTOs.ResponseDto;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CinemaApi.Services
{
    // Unico punto che parla con TMDB (https://developer.themoviedb.org).
    // Il token resta sul server: il FE chiama solo le nostre API.
    // Il token va negli User Secrets: dotnet user-secrets set "Tmdb:Token" "<token di lettura v4>"
    public class TmdbService
    {
        private const string UrlImmagini = "https://image.tmdb.org/t/p/";
        private const string Lingua = "it-IT";

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

        public async Task<TmdbRicercaDto> Cerca(string testo, int pagina = 1, int? anno = null)
        {
            var chiave = $"tmdb:cerca:{testo.ToLowerInvariant()}:{pagina}:{anno}";
            if (_cache.TryGetValue(chiave, out TmdbRicercaDto? inCache) && inCache != null) return Copia(inCache);

            var url = $"search/movie?query={Uri.EscapeDataString(testo)}&page={pagina}&language={Lingua}&include_adult=false";
            if (anno != null) url += $"&primary_release_year={anno}";

            var risposta = await Get<RispostaRicerca>(url)
                ?? new RispostaRicerca();

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
                    LocandinaUrl = Immagine(r.PosterPath, "w342"),
                    Descrizione = r.Overview
                }).ToList()
            };

            _cache.Set(chiave, risultato, TimeSpan.FromMinutes(10));
            return Copia(risultato);
        }

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
