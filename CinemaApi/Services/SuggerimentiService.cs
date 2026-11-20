using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // "Potrebbero piacerti": film o serie scelti in base a quello che l'utente ha visto e votato.
    // I titoli di partenza sono quelli che gli sono piaciuti di più (voto alto, poi visti/seguiti
    // di recente); per ognuno TMDB dà i suoi consigliati. Un titolo consigliato da più titoli
    // di partenza, o da titoli votati meglio, sale in classifica. Si tolgono quelli che l'utente
    // ha già visto, votato o messo in "Da vedere".
    public class SuggerimentiService
    {
        public const int TitoliDiPartenza = 8;
        public const int Risultati = 18;

        private readonly CinemaContext _db;
        private readonly TmdbService _tmdb;

        public SuggerimentiService(CinemaContext db, TmdbService tmdb)
        {
            _db = db;
            _tmdb = tmdb;
        }

        private record Partenza(int TmdbId, string Titolo, double Peso, bool Piaciuto, DateTime Data);

        public async Task<List<TitoloPersonaDto>> Suggerimenti(int idUtente, string tipo)
        {
            var titoli = _db.Film.Where(f => f.Tipo == tipo && f.TmdbId != null);

            var voti = await _db.VotiFilm
                .Where(v => v.IdUtente == idUtente && titoli.Any(f => f.IdFilm == v.IdFilm))
                .Select(v => new { TmdbId = v.Film!.TmdbId!.Value, v.Film.Titolo, v.Voto, Data = v.DataVoto })
                .ToListAsync();

            // Film visti, oppure serie con almeno un episodio visto (più episodi = più peso)
            var visti = tipo == TipiTitolo.Serie
                ? await _db.EpisodiVisti
                    .Where(e => e.IdUtente == idUtente && titoli.Any(f => f.IdFilm == e.IdFilm))
                    .GroupBy(e => new { TmdbId = e.Film!.TmdbId!.Value, e.Film.Titolo })
                    .Select(g => new { g.Key.TmdbId, g.Key.Titolo, Quanti = g.Count(), Data = g.Max(e => e.DataCreazione) })
                    .ToListAsync()
                : await _db.Visioni
                    .Where(v => v.IdUtente == idUtente && titoli.Any(f => f.IdFilm == v.IdFilm))
                    .GroupBy(v => new { TmdbId = v.Film!.TmdbId!.Value, v.Film.Titolo })
                    .Select(g => new { g.Key.TmdbId, g.Key.Titolo, Quanti = 1, Data = g.Max(v => v.DataCreazione) })
                    .ToListAsync();

            var daVedere = await _db.DaVedere
                .Where(d => d.IdUtente == idUtente && titoli.Any(f => f.IdFilm == d.IdFilm))
                .Select(d => d.Film!.TmdbId!.Value)
                .ToListAsync();

            // Il voto conta più del solo "visto"; un voto basso esclude il titolo dalla partenza
            var partenze = new Dictionary<int, Partenza>();
            foreach (var v in visti)
                partenze[v.TmdbId] = new Partenza(v.TmdbId, v.Titolo ?? "", 1 + Math.Min(v.Quanti / 10.0, 1), false, v.Data);
            foreach (var v in voti)
            {
                if (v.Voto <= 2) { partenze.Remove(v.TmdbId); continue; }
                var peso = v.Voto switch { 5 => 3.0, 4 => 2.0, _ => 0.5 };
                partenze[v.TmdbId] = new Partenza(v.TmdbId, v.Titolo ?? "", peso, v.Voto >= 4, v.Data);
            }

            var scelte = partenze.Values
                .OrderByDescending(p => p.Peso).ThenByDescending(p => p.Data)
                .Take(TitoliDiPartenza)
                .ToList();
            if (scelte.Count == 0) return new List<TitoloPersonaDto>();

            var giaSuoi = new HashSet<int>(visti.Select(v => v.TmdbId).Concat(voti.Select(v => v.TmdbId)).Concat(daVedere));

            // TMDB in parallelo (le risposte restano in cache 12 ore)
            var consigliati = await Task.WhenAll(scelte.Select(p => _tmdb.Consigliati(p.TmdbId, tipo)));

            var punteggi = new Dictionary<int, (TmdbRisultatoDto Titolo, double Punti, Partenza Motivo, double MigliorContributo)>();
            foreach (var (partenza, lista) in scelte.Zip(consigliati))
            {
                for (var i = 0; i < lista.Count; i++)
                {
                    var t = lista[i];
                    if (giaSuoi.Contains(t.TmdbId) || partenze.ContainsKey(t.TmdbId)) continue;
                    // I primi consigliati di ogni titolo contano di più
                    var contributo = partenza.Peso * (1 - i / 25.0);
                    if (punteggi.TryGetValue(t.TmdbId, out var c))
                        punteggi[t.TmdbId] = (c.Titolo, c.Punti + contributo,
                            contributo > c.MigliorContributo ? partenza : c.Motivo, Math.Max(contributo, c.MigliorContributo));
                    else
                        punteggi[t.TmdbId] = (t, contributo, partenza, contributo);
                }
            }

            return punteggi.Values
                .OrderByDescending(p => p.Punti).ThenByDescending(p => p.Titolo.Popolarita)
                .Take(Risultati)
                .Select(p => new TitoloPersonaDto
                {
                    TmdbId = p.Titolo.TmdbId,
                    Tipo = p.Titolo.Tipo,
                    Titolo = p.Titolo.Titolo,
                    TitoloOriginale = p.Titolo.TitoloOriginale,
                    AnnoUscita = p.Titolo.AnnoUscita,
                    DataUscita = p.Titolo.DataUscita,
                    LocandinaUrl = p.Titolo.LocandinaUrl,
                    Descrizione = p.Titolo.Descrizione,
                    // Il FE mostra Ruolo sotto il titolo della copertina: qui è il motivo del suggerimento
                    Ruolo = p.Motivo.Piaciuto ? $"Perché ti è piaciuto {p.Motivo.Titolo}" : $"Perché hai visto {p.Motivo.Titolo}"
                })
                .ToList();
        }
    }
}
