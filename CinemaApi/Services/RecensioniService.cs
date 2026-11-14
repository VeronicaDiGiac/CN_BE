using CinemaApi.DTOs.RequestDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    public class RecensioniService
    {
        private readonly CinemaContext _db;
        private readonly NotificheService _notifiche;

        public RecensioniService(CinemaContext db, NotificheService notifiche)
        {
            _db = db;
            _notifiche = notifiche;
        }

        public async Task<Recensione?> GetRecensioneById(int id)
        {
            return await _db.Recensioni.FindAsync(id);
        }

        // idUtenteAutenticato serve solo a valorizzare MiPiaceUtente (null per chi non è loggato).
        // Le più apprezzate prima, poi le più recenti.
        public async Task<List<RecensioneDto>> GetRecensioni(int? idFilm, int? idUtente, int? idUtenteAutenticato = null)
        {
            var recensioni = _db.Recensioni
                .Where(r => idFilm == null || r.IdFilm == idFilm)
                .Where(r => idUtente == null || r.IdUtente == idUtente);
            return await InDto(recensioni, idUtenteAutenticato)
                .OrderByDescending(r => r.NumeroMiPiace)
                .ThenByDescending(r => r.DataRecensione)
                .ToListAsync();
        }

        // Una sola recensione, per la sua pagina (null se non esiste)
        public Task<RecensioneDto?> GetRecensione(int id, int? idUtenteAutenticato) =>
            InDto(_db.Recensioni.Where(r => r.IdRecensione == id), idUtenteAutenticato).FirstOrDefaultAsync();

        // Proiezione comune a elenco e singola recensione: EF la traduce in un'unica query SQL
        private static IQueryable<RecensioneDto> InDto(IQueryable<Recensione> recensioni, int? idUtenteAutenticato) =>
            recensioni.Select(r => new RecensioneDto
            {
                IdRecensione = r.IdRecensione,
                IdUtente = r.IdUtente,
                IdFilm = r.IdFilm,
                UserName = r.Utente != null ? r.Utente.UserName : null,
                AutoreAdmin = r.Utente != null && r.Utente.Ruolo == Ruoli.Admin,
                TitoloFilm = r.Film != null ? r.Film.Titolo : null,
                ImmagineUrlFilm = r.Film != null ? r.Film.ImmagineUrl : null,
                Voto = r.Voto,
                Contenuto = r.Contenuto,
                DataRecensione = r.DataRecensione,
                Spoiler = r.Spoiler,
                NumeroMiPiace = r.MiPiace!.Count(),
                MiPiaceUtente = idUtenteAutenticato != null && r.MiPiace!.Any(m => m.IdUtente == idUtenteAutenticato),
                NumeroCommenti = r.Commenti!.Count()
            });

        // Idempotente: un secondo "mi piace" non cambia niente (e non manda un'altra notifica)
        public async Task MettiMiPiace(Recensione recensione, int idUtente)
        {
            if (await _db.MiPiaceRecensioni.AnyAsync(m => m.IdRecensione == recensione.IdRecensione && m.IdUtente == idUtente)) return;

            _db.MiPiaceRecensioni.Add(new MiPiaceRecensione
            {
                IdRecensione = recensione.IdRecensione,
                IdUtente = idUtente,
                DataMiPiace = DateTime.Now
            });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Due clic contemporanei: la chiave primaria ha già evitato il doppione
                return;
            }
            await _notifiche.Notifica(recensione.IdUtente, idUtente, TipiNotifica.MiPiaceRecensione,
                recensione.IdFilm, recensione.IdRecensione);
        }

        public async Task TogliMiPiace(Recensione recensione, int idUtente)
        {
            var tolti = await _db.MiPiaceRecensioni
                .Where(m => m.IdRecensione == recensione.IdRecensione && m.IdUtente == idUtente)
                .ExecuteDeleteAsync();
            if (tolti > 0)
                await _notifiche.Annulla(recensione.IdUtente, idUtente, TipiNotifica.MiPiaceRecensione, recensione.IdRecensione);
        }

        public async Task<RecensioneDto?> CreateRecensione(CreateRecensioneDto dto, int idUtente)
        {
            var esistente = await _db.Recensioni.FirstOrDefaultAsync(r => r.IdUtente == idUtente && r.IdFilm == dto.IdFilm);
            if (esistente != null) return null;
            var recensione = new Recensione
            {
                IdUtente = idUtente,
                IdFilm = dto.IdFilm,
                Voto = dto.Voto,
                Contenuto = dto.Contenuto,
                DataRecensione = DateTime.Now,
                Spoiler = dto.Spoiler
            };
            _db.Recensioni.Add(recensione);
            await _db.SaveChangesAsync();
            return new RecensioneDto
            {
                IdRecensione = recensione.IdRecensione,
                IdUtente = recensione.IdUtente,
                IdFilm = recensione.IdFilm,
                UserName = await _db.Utenti.Where(u => u.IdUtente == idUtente).Select(u => u.UserName).FirstOrDefaultAsync(),
                AutoreAdmin = await _db.Utenti.AnyAsync(u => u.IdUtente == idUtente && u.Ruolo == Ruoli.Admin),
                TitoloFilm = await _db.Film.Where(f => f.IdFilm == dto.IdFilm).Select(f => f.Titolo).FirstOrDefaultAsync(),
                Voto = recensione.Voto,
                Contenuto = recensione.Contenuto,
                DataRecensione = recensione.DataRecensione,
                Spoiler = recensione.Spoiler
            };
        }

        public async Task<bool> UpdateRecensione(int id, CreateRecensioneDto dto)
        {
            var recensione = await _db.Recensioni.FindAsync(id);
            if (recensione == null) return false;

            recensione.Voto = dto.Voto;
            recensione.Contenuto = dto.Contenuto;
            recensione.Spoiler = dto.Spoiler;

            await _db.SaveChangesAsync();
            return true;
        }

        // Si possono recensire solo i film approvati (gli altri non sono visibili).
        public async Task<bool> FilmEsiste(int idFilm)
        {
            return await _db.FilmApprovatoEsiste(idFilm);
        }

        // Elimina la recensione insieme a tutti i suoi commenti (vedi EliminazioneHelper).
        public async Task<bool> DeleteRecensione(int id)
        {
            var recensione = await _db.Recensioni.FindAsync(id);
            if (recensione == null) return false;

            using var transazione = await _db.Database.BeginTransactionAsync();
            await EliminazioneHelper.EliminaRecensioni(_db, new List<int> { id });
            await transazione.CommitAsync();
            return true;
        }
    }
}