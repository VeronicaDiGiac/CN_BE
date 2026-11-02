using CinemaApi.Helpers;
using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
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

        public async Task<Preferito?> GetPreferitoById(int id)
        {
            return await _db.Preferiti.FindAsync(id);
        }



        public async Task<List<PreferitoDto>> GetPreferiti(int? idUtente)
        {
            return await Proietta(_db.Preferiti.Where(p => idUtente == null || p.IdUtente == idUtente))
                .ToListAsync();
        }

        private static IQueryable<PreferitoDto> Proietta(IQueryable<Preferito> preferiti) =>
            preferiti.Select(p => new PreferitoDto
            {
                IdPreferito = p.IdPreferito,
                IdUtente = p.IdUtente,
                IdFilm = p.IdFilm,
                TitoloFilm = p.Film != null ? p.Film.Titolo : null,
                Regista = p.Film != null ? p.Film.Regista : null,
                DataPreferito = p.DataPreferito
            });

        // Si possono aggiungere ai preferiti solo i film approvati (gli altri non sono visibili).
        public async Task<bool> FilmEsiste(int idFilm)
        {
            return await _db.FilmApprovatoEsiste(idFilm);
        }

        public async Task<PreferitoDto?> CreatePreferito(CreatePreferitoDto dto, int idUtente)
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
            return await Proietta(_db.Preferiti.Where(p => p.IdPreferito == preferito.IdPreferito)).FirstAsync();
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