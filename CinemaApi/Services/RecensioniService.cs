using CinemaApi.DTOs.RequestDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    public class RecensioniService
    {
        private readonly CinemaContext _db;

        public RecensioniService(CinemaContext db)
        {
            _db = db;
        }

        public async Task<Recensione?> GetRecensioneById(int id)
        {
            return await _db.Recensioni.FindAsync(id);
        }

        // idUtenteAutenticato serve solo a valorizzare MiPiaceUtente (null per chi non è loggato).
        // Le più apprezzate prima, poi le più recenti.
        public async Task<List<RecensioneDto>> GetRecensioni(int? idFilm, int? idUtente, int? idUtenteAutenticato = null)
        {
            return await _db.Recensioni
                .Where(r => idFilm == null || r.IdFilm == idFilm)
                .Where (r => idUtente == null || r.IdUtente == idUtente)
                .Select(r => new RecensioneDto
                {
                    IdRecensione = r.IdRecensione,
                    IdUtente = r.IdUtente,
                    IdFilm = r.IdFilm,
                    UserName = r.Utente != null ? r.Utente.UserName : null,
                    TitoloFilm = r.Film != null ? r.Film.Titolo : null,
                    Voto = r.Voto,
                    Contenuto = r.Contenuto,
                    DataRecensione = r.DataRecensione,
                    Spoiler = r.Spoiler,
                    NumeroMiPiace = r.MiPiace!.Count(),
                    MiPiaceUtente = idUtenteAutenticato != null && r.MiPiace!.Any(m => m.IdUtente == idUtenteAutenticato)
                })
                .OrderByDescending(r => r.NumeroMiPiace)
                .ThenByDescending(r => r.DataRecensione)
                .ToListAsync();
        }

        // Idempotente: un secondo "mi piace" non cambia niente
        public async Task MettiMiPiace(int idRecensione, int idUtente)
        {
            if (await _db.MiPiaceRecensioni.AnyAsync(m => m.IdRecensione == idRecensione && m.IdUtente == idUtente)) return;

            _db.MiPiaceRecensioni.Add(new MiPiaceRecensione
            {
                IdRecensione = idRecensione,
                IdUtente = idUtente,
                DataMiPiace = DateTime.UtcNow
            });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Due clic contemporanei: la chiave primaria ha già evitato il doppione
            }
        }

        public async Task TogliMiPiace(int idRecensione, int idUtente) =>
            await _db.MiPiaceRecensioni
                .Where(m => m.IdRecensione == idRecensione && m.IdUtente == idUtente)
                .ExecuteDeleteAsync();

        public async Task<Recensione?> CreateRecensione(CreateRecensioneDto dto, int idUtente)
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
            return recensione;
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