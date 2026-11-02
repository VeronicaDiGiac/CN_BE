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

        public async Task<List<RecensioneDto>> GetRecensioni(int? idFilm, int? idUtente)
        {
            return await _db.Recensioni
                .Include(r => r.Utente)
                .Include(r => r.Film)
                .Where(r => idFilm == null || r.IdFilm == idFilm)
                .Where (r => idUtente == null || r.IdUtente == idUtente)
                .Select(r => new RecensioneDto
                {
                    IdRecensione = r.IdRecensione,
                    IdUtente = r.IdUtente,
                    UserName = r.Utente != null ? r.Utente.UserName : null,
                    TitoloFilm = r.Film != null ? r.Film.Titolo : null,
                    Voto = r.Voto,
                    Contenuto = r.Contenuto,
                    DataRecensione = r.DataRecensione
                })
                .ToListAsync();
        }

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
                DataRecensione = DateTime.Now
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

            await _db.SaveChangesAsync();
            return true;
        }

        // Si possono recensire solo i film approvati (gli altri non sono visibili).
        public async Task<bool> FilmEsiste(int idFilm)
        {
            return await _db.Film.AnyAsync(f => f.IdFilm == idFilm && f.Stato == StatiFilm.Approvato);
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