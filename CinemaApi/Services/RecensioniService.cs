using CinemaApi.DTOs.RequestDto;
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

        public async Task<Recensione> CreateRecensione(CreateRecensioneDto dto)
        {
            var esistente = await _db.Recensioni.FirstOrDefaultAsync(r => r.IdUtente == dto.IdUtente && r.IdFilm == dto.IdFilm);
            if (esistente != null) return null;
            var recensione = new Recensione
            {
                IdUtente = dto.IdUtente,
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

        public async Task<bool> DeleteRecensione(int id)
        {
            var recensione = await _db.Recensioni.FindAsync(id);
            if (recensione == null) return false;

            _db.Recensioni.Remove(recensione);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}