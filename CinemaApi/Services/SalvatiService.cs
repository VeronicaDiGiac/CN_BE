using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Recensioni e discussioni salvate dall'utente (segnalibro). Salvare e togliere sono idempotenti.
    public class SalvatiService
    {
        private readonly CinemaContext _db;
        public SalvatiService(CinemaContext db) { _db = db; }

        // "recensione"/"Recensione" → TipiContenuto.Recensione…; null se il tipo non si può salvare
        public static string? Normalizza(string? tipo) =>
            string.Equals(tipo, TipiContenuto.Recensione, StringComparison.OrdinalIgnoreCase) ? TipiContenuto.Recensione
            : string.Equals(tipo, TipiContenuto.Discussione, StringComparison.OrdinalIgnoreCase) ? TipiContenuto.Discussione
            : null;

        public Task<bool> ContenutoEsiste(string tipo, int id) =>
            tipo == TipiContenuto.Recensione
                ? _db.Recensioni.AnyAsync(r => r.IdRecensione == id)
                : _db.Discussioni.AnyAsync(d => d.IdDiscussione == id);

        public async Task Salva(int idUtente, string tipo, int id)
        {
            var gia = await _db.Salvati.AnyAsync(s => s.IdUtente == idUtente && s.TipoContenuto == tipo && s.IdContenuto == id);
            if (gia) return;
            _db.Salvati.Add(new Salvato { IdUtente = idUtente, TipoContenuto = tipo, IdContenuto = id, DataSalvataggio = DateTime.Now });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Due clic contemporanei: la chiave primaria ha già evitato il doppione
            }
        }

        public Task Togli(int idUtente, string tipo, int id) =>
            _db.Salvati.Where(s => s.IdUtente == idUtente && s.TipoContenuto == tipo && s.IdContenuto == id).ExecuteDeleteAsync();

        public Task<List<ChiaveSalvatoDto>> GetChiavi(int idUtente) =>
            _db.Salvati
                .Where(s => s.IdUtente == idUtente)
                .Select(s => new ChiaveSalvatoDto { Tipo = s.TipoContenuto, IdContenuto = s.IdContenuto })
                .ToListAsync();

        // I salvati dell'utente, dall'ultimo salvato; tipo null = recensioni e discussioni insieme
        public async Task<PagedResultDto<SalvatoDto>> GetSalvati(int idUtente, string? tipo, int pageNumber, int pageSize)
        {
            var query = _db.Salvati.Where(s => s.IdUtente == idUtente);
            if (tipo != null) query = query.Where(s => s.TipoContenuto == tipo);

            var totale = await query.CountAsync();
            var pagina = await query
                .OrderByDescending(s => s.DataSalvataggio)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            var idRecensioni = pagina.Where(s => s.TipoContenuto == TipiContenuto.Recensione).Select(s => s.IdContenuto).ToList();
            var idDiscussioni = pagina.Where(s => s.TipoContenuto == TipiContenuto.Discussione).Select(s => s.IdContenuto).ToList();

            var recensioni = await _db.Recensioni
                .Where(r => idRecensioni.Contains(r.IdRecensione))
                .Select(r => new SalvatoDto
                {
                    Tipo = TipiContenuto.Recensione,
                    IdContenuto = r.IdRecensione,
                    IdFilm = r.IdFilm,
                    TitoloFilm = r.Film != null ? r.Film.Titolo : null,
                    ImmagineFilm = r.Film != null ? r.Film.ImmagineUrl : null,
                    IdAutore = r.IdUtente,
                    Autore = r.Utente != null ? r.Utente.UserName : null,
                    Testo = r.Contenuto,
                    Spoiler = r.Spoiler,
                    Voto = r.Voto,
                    NumeroMiPiace = r.MiPiace!.Count(),
                    NumeroCommenti = r.Commenti!.Count()
                })
                .ToDictionaryAsync(x => x.IdContenuto);

            var discussioni = await _db.Discussioni
                .Where(d => idDiscussioni.Contains(d.IdDiscussione))
                .Select(d => new SalvatoDto
                {
                    Tipo = TipiContenuto.Discussione,
                    IdContenuto = d.IdDiscussione,
                    IdFilm = d.IdFilm,
                    TitoloFilm = d.Film != null ? d.Film.Titolo : null,
                    ImmagineFilm = d.Film != null ? d.Film.ImmagineUrl : null,
                    IdAutore = d.IdUtente,
                    Autore = d.Utente != null ? d.Utente.UserName : null,
                    Titolo = d.Titolo,
                    Testo = d.Contenuto,
                    Spoiler = d.Spoiler,
                    Stagione = d.Stagione,
                    Episodio = d.Episodio,
                    NumeroMiPiace = d.MiPiace!.Count(),
                    NumeroCommenti = d.Commenti!.Count()
                })
                .ToDictionaryAsync(x => x.IdContenuto);

            // Stesso ordine dei salvataggi; un contenuto eliminato nel frattempo non compare
            var voci = new List<SalvatoDto>();
            foreach (var s in pagina)
            {
                var elenco = s.TipoContenuto == TipiContenuto.Recensione ? recensioni : discussioni;
                if (!elenco.TryGetValue(s.IdContenuto, out var voce)) continue;
                voce.DataSalvataggio = s.DataSalvataggio;
                voci.Add(voce);
            }

            return new PagedResultDto<SalvatoDto> { TotalCount = totale, PageNumber = pageNumber, PageSize = pageSize, Items = voci };
        }
    }
}
