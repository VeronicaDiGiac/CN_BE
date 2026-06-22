using CinemaApi.DTOs.RequestDto;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    public class PreferitiService
    {
        private readonly CinemaContext _db;

        public PreferitiService(CinemaContext db)
        {
            _db = db;
        }

        public async Task<List<object>> GetPreferiti(int? idUtente)
        {
            return await _db.Preferiti
                .Include(p => p.Film)
                .Where(p => idUtente == null || p.IdUtente == idUtente)
                .Select(p => new
                {
                    p.IdPreferito,
                    p.IdUtente,
                    p.IdFilm,
                    TitoloFilm = p.Film != null ? p.Film.Titolo : null,
                    Regista = p.Film != null ? p.Film.Regista : null,
                    p.DataPreferito
                } as object)
                .ToListAsync();
        }

        public async Task<Preferito?> CreatePreferito(CreatePreferitoDto dto, int idUtente)
        {
            var esistente = await _db.Preferiti
                .FirstOrDefaultAsync(p => p.IdUtente == idUtente && p.IdFilm == dto.IdFilm);

            if (esistente != null) return null;

            var preferito = new Preferito
            {
                IdUtente = idUtente,
                IdFilm = dto.IdFilm,
                DataPreferito = DateTime.Now
            };
            _db.Preferiti.Add(preferito);
            await _db.SaveChangesAsync();
            return preferito;
        }

        public async Task<bool> DeletePreferito(int id)
        {
            var preferito = await _db.Preferiti.FindAsync(id);
            if (preferito == null) return false;

            _db.Preferiti.Remove(preferito);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}