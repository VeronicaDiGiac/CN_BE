using System.Text.Json;
using System.Text.RegularExpressions;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CinemaApi.Services
{
    // "Curiosità sulla produzione" di un film o di una serie, dalla pagina Wikipedia:
    // 1. Wikidata trova l'elemento dall'id TMDB (P4947 film, P4983 serie) e le sue pagine it/en;
    // 2. Wikipedia restituisce il testo semplice (estratto) con i titoli delle sezioni;
    // 3. si tengono "Produzione"/"Production" (con Sviluppo, Riprese, Filming…) e "Curiosità"/"Trivia".
    // Prima l'italiano; se la sezione italiana è troppo corta, l'inglese. Il testo è CC BY-SA:
    // il FE cita la fonte con il link alla pagina. Risultati (anche vuoti) in cache.
    public class WikipediaService
    {
        public const int SezioniMax = 6;
        public const int CaratteriPerSezione = 700;
        // Sotto questa lunghezza la parte italiana è troppo povera: si prova l'inglese
        public const int CaratteriMinimiItaliano = 600;

        private static readonly string[] TitoliProduzione = { "produzione", "production", "realizzazione" };
        private static readonly string[] TitoliCuriosita = { "curiosità", "trivia" };

        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly CinemaContext _db;
        private readonly ILogger<WikipediaService> _logger;

        public WikipediaService(HttpClient http, IMemoryCache cache, CinemaContext db, ILogger<WikipediaService> logger)
        {
            _http = http;
            _cache = cache;
            _db = db;
            _logger = logger;
        }

        public Task<bool> FilmEsiste(int idFilm) => _db.FilmApprovatoEsiste(idFilm);

        // null se il film non è su TMDB, non ha una pagina Wikipedia o la pagina non parla della produzione
        public async Task<CuriositaDto?> Curiosita(int idFilm)
        {
            var film = await _db.Film.Where(f => f.IdFilm == idFilm)
                .Select(f => new { f.TmdbId, f.Tipo }).FirstOrDefaultAsync();
            if (film?.TmdbId == null) return null;

            var chiave = $"wiki:curiosita:{film.Tipo}:{film.TmdbId}";
            if (_cache.TryGetValue(chiave, out CuriositaDto? inCache)) return inCache;

            CuriositaDto? risultato = null;
            try
            {
                risultato = await Cerca(film.TmdbId.Value, film.Tipo);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                // Wikipedia non risponde: niente curiosità questa volta, si riprova tra poco
                _logger.LogWarning(ex, "Curiosità da Wikipedia non disponibili (TMDB {TmdbId})", film.TmdbId);
                _cache.Set(chiave, (CuriositaDto?)null, TimeSpan.FromMinutes(10));
                return null;
            }
            _cache.Set(chiave, risultato, TimeSpan.FromHours(risultato == null ? 6 : 24));
            return risultato;
        }

        private async Task<CuriositaDto?> Cerca(int tmdbId, string tipo)
        {
            var proprieta = tipo == TipiTitolo.Serie ? "P4983" : "P4947";
            var ricerca = await Json($"https://www.wikidata.org/w/api.php?action=query&list=search&format=json&srlimit=1&srsearch=haswbstatement:{proprieta}={tmdbId}");
            var qid = ricerca?.RootElement.GetProperty("query").GetProperty("search").EnumerateArray()
                .Select(r => r.GetProperty("title").GetString()).FirstOrDefault();
            if (string.IsNullOrEmpty(qid)) return null;

            var entita = await Json($"https://www.wikidata.org/w/api.php?action=wbgetentities&format=json&props=sitelinks&sitefilter=itwiki|enwiki&ids={qid}");
            if (entita == null || !entita.RootElement.GetProperty("entities").GetProperty(qid).TryGetProperty("sitelinks", out var link))
                return null;

            CuriositaDto? italiano = null;
            if (link.TryGetProperty("itwiki", out var it))
            {
                italiano = await DaPagina("it", it.GetProperty("title").GetString()!);
                if (italiano != null && italiano.Sezioni.Sum(s => s.Testo.Length) >= CaratteriMinimiItaliano) return italiano;
            }
            if (link.TryGetProperty("enwiki", out var en))
            {
                var inglese = await DaPagina("en", en.GetProperty("title").GetString()!);
                if (inglese != null) return inglese;
            }
            return italiano;
        }

        private async Task<CuriositaDto?> DaPagina(string lingua, string titolo)
        {
            var risposta = await Json($"https://{lingua}.wikipedia.org/w/api.php?action=query&format=json&prop=extracts&explaintext=1&exsectionformat=wiki&redirects=1&titles={Uri.EscapeDataString(titolo)}");
            var pagina = risposta?.RootElement.GetProperty("query").GetProperty("pages").EnumerateObject().First().Value;
            if (pagina == null || !pagina.Value.TryGetProperty("extract", out var estratto)) return null;

            var sezioni = SezioniUtili(estratto.GetString() ?? "");
            if (sezioni.Count == 0) return null;

            var titoloPagina = pagina.Value.GetProperty("title").GetString() ?? titolo;
            return new CuriositaDto
            {
                Lingua = lingua,
                TitoloPagina = titoloPagina,
                Url = $"https://{lingua}.wikipedia.org/wiki/{Uri.EscapeDataString(titoloPagina.Replace(' ', '_'))}",
                Sezioni = sezioni
            };
        }

        // Dal testo con i titoli "== Produzione ==" / "=== Riprese ===" tiene la produzione
        // (testo iniziale e sottosezioni) e le curiosità; ogni parte accorciata a fine frase
        private static List<SezioneCuriositaDto> SezioniUtili(string testo)
        {
            var parti = new List<(int Livello, string Titolo, string Testo)>();
            var titolo = Regex.Matches(testo, @"^(={2,4})\s*(.+?)\s*\1\s*$", RegexOptions.Multiline);
            for (var i = 0; i < titolo.Count; i++)
            {
                var inizio = titolo[i].Index + titolo[i].Length;
                var fine = i + 1 < titolo.Count ? titolo[i + 1].Index : testo.Length;
                parti.Add((titolo[i].Groups[1].Length, titolo[i].Groups[2].Value.Trim(), testo[inizio..fine].Trim()));
            }

            var risultato = new List<SezioneCuriositaDto>();
            for (var i = 0; i < parti.Count && risultato.Count < SezioniMax; i++)
            {
                var (livello, nome, corpo) = parti[i];
                if (livello != 2) continue;
                var minuscolo = nome.ToLowerInvariant();
                var produzione = TitoliProduzione.Contains(minuscolo);
                if (!produzione && !TitoliCuriosita.Contains(minuscolo)) continue;

                if (Pulito(corpo) is { Length: > 0 } introduzione)
                    risultato.Add(new SezioneCuriositaDto { Titolo = nome, Testo = Accorcia(introduzione) });
                // Sottosezioni fino alla prossima sezione di livello 2
                for (var j = i + 1; j < parti.Count && parti[j].Livello > 2 && risultato.Count < SezioniMax; j++)
                {
                    if (Pulito(parti[j].Testo) is { Length: > 0 } sotto)
                        risultato.Add(new SezioneCuriositaDto { Titolo = parti[j].Titolo, Testo = Accorcia(sotto) });
                }
            }
            return risultato;
        }

        // Righe vuote multiple e spazi in eccesso via
        private static string Pulito(string testo) =>
            Regex.Replace(Regex.Replace(testo, @"[ \t]+", " "), @"\n{2,}", "\n").Trim();

        private static string Accorcia(string testo)
        {
            if (testo.Length <= CaratteriPerSezione) return testo;
            var taglio = testo[..CaratteriPerSezione];
            var punto = taglio.LastIndexOf(". ", StringComparison.Ordinal);
            return (punto > CaratteriPerSezione / 2 ? taglio[..(punto + 1)] : taglio.TrimEnd()) + " …";
        }

        private async Task<JsonDocument?> Json(string url)
        {
            using var risposta = await _http.GetAsync(url);
            if (!risposta.IsSuccessStatusCode) return null;
            await using var flusso = await risposta.Content.ReadAsStreamAsync();
            return await JsonDocument.ParseAsync(flusso);
        }
    }
}
