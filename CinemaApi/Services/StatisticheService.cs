using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Statistiche personali e "riepilogo dell'anno". I dati di un utente sono pochi (il suo diario):
    // si leggono con una query e si aggregano in memoria, più semplice che farlo in SQL
    // (generi e registi sono testi con più valori separati da virgola).
    public class StatisticheService
    {
        private const int VociClassifica = 5;
        private static readonly string[] Mesi =
            { "Gen", "Feb", "Mar", "Apr", "Mag", "Giu", "Lug", "Ago", "Set", "Ott", "Nov", "Dic" };

        private readonly CinemaContext _db;

        public StatisticheService(CinemaContext db) { _db = db; }

        public Task<bool> UtenteEsiste(int idUtente) => _db.Utenti.AnyAsync(u => u.IdUtente == idUtente);

        public async Task<StatisticheDto> GetStatistiche(int idUtente, int? anno)
        {
            var anni = await _db.Visioni
                .Where(v => v.IdUtente == idUtente)
                .Select(v => v.DataVisione.Year)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();

            var visioni = await _db.Visioni
                .Where(v => v.IdUtente == idUtente && (anno == null || v.DataVisione.Year == anno))
                .Select(v => new
                {
                    v.IdFilm,
                    v.DataVisione,
                    v.IdVisione,
                    v.Film!.Titolo,
                    v.Film.ImmagineUrl,
                    v.Film.Genere,
                    v.Film.Regista,
                    v.Film.DurataMinuti
                })
                .ToListAsync();

            var recensioni = await _db.Recensioni
                .Where(r => r.IdUtente == idUtente && r.Voto != null
                            && (anno == null || (r.DataRecensione != null && r.DataRecensione.Value.Year == anno)))
                .Select(r => new { r.IdFilm, r.Voto, r.DataRecensione, r.Film!.Titolo, r.Film.ImmagineUrl })
                .ToListAsync();

            var stat = new StatisticheDto
            {
                Anno = anno,
                AnniDisponibili = anni,
                TotaleVisioni = visioni.Count,
                FilmDiversi = visioni.Select(v => v.IdFilm).Distinct().Count(),
                MinutiTotali = visioni.Sum(v => v.DurataMinuti ?? 0),
                VisioniConDurata = visioni.Count(v => v.DurataMinuti != null),
                Recensioni = recensioni.Count,
                VotoMedio = recensioni.Count > 0 ? Math.Round(recensioni.Average(r => r.Voto!.Value), 1) : null,
                // Ogni film conta una volta anche se rivisto: "i generi che ho visto", non "quante volte"
                GeneriTop = Classifica(visioni.DistinctBy(v => v.IdFilm).Select(v => v.Genere)),
                RegistiTop = Classifica(visioni.DistinctBy(v => v.IdFilm).Select(v => v.Regista))
            };

            stat.Andamento = anno != null
                ? Enumerable.Range(1, 12)
                    .Select(m => new VoceClassificaDto { Nome = Mesi[m - 1], Conteggio = visioni.Count(v => v.DataVisione.Month == m) })
                    .ToList()
                : visioni.GroupBy(v => v.DataVisione.Year)
                    .OrderBy(g => g.Key)
                    .Select(g => new VoceClassificaDto { Nome = g.Key.ToString(), Conteggio = g.Count() })
                    .ToList();

            var piuVisto = visioni.GroupBy(v => v.IdFilm)
                .Where(g => g.Count() > 1)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            if (piuVisto != null)
            {
                var f = piuVisto.First();
                stat.FilmPiuVisto = new FilmStatisticaDto { IdFilm = f.IdFilm, Titolo = f.Titolo, ImmagineUrl = f.ImmagineUrl, Dettaglio = piuVisto.Count().ToString() };
            }

            var votoPiuAlto = recensioni.OrderByDescending(r => r.Voto).ThenByDescending(r => r.DataRecensione).FirstOrDefault();
            if (votoPiuAlto != null)
                stat.FilmVotoPiuAlto = new FilmStatisticaDto { IdFilm = votoPiuAlto.IdFilm, Titolo = votoPiuAlto.Titolo, ImmagineUrl = votoPiuAlto.ImmagineUrl, Dettaglio = votoPiuAlto.Voto.ToString() };

            var ordinate = visioni.OrderBy(v => v.DataVisione).ThenBy(v => v.IdVisione).ToList();
            if (ordinate.Count > 0)
            {
                var prima = ordinate[0];
                var ultima = ordinate[^1];
                stat.PrimaVisione = new FilmStatisticaDto { IdFilm = prima.IdFilm, Titolo = prima.Titolo, ImmagineUrl = prima.ImmagineUrl, Dettaglio = prima.DataVisione.ToString("yyyy-MM-dd") };
                stat.UltimaVisione = new FilmStatisticaDto { IdFilm = ultima.IdFilm, Titolo = ultima.Titolo, ImmagineUrl = ultima.ImmagineUrl, Dettaglio = ultima.DataVisione.ToString("yyyy-MM-dd") };
            }

            return stat;
        }

        // "Fantascienza, Avventura" -> conta ogni valore; prime VociClassifica, a parità in ordine alfabetico
        private static List<VoceClassificaDto> Classifica(IEnumerable<string?> testi) =>
            testi
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .SelectMany(t => t!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
                .Select(g => new VoceClassificaDto { Nome = g.First(), Conteggio = g.Count() })
                .OrderByDescending(v => v.Conteggio)
                .ThenBy(v => v.Nome)
                .Take(VociClassifica)
                .ToList();
    }
}
