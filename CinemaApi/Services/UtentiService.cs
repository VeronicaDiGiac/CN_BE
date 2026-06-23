using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    public class UtentiService


    {

        private readonly CinemaContext _db;
        public UtentiService(CinemaContext db)
        {
            _db = db;
        }

        public async Task<bool> UpdateProfilo(int id, UpdateProfiloDto dto)
        {
            var utente = await _db.Utenti.FindAsync(id);

            if (dto.UserName == null)
            {
                
                    utente.UserName = dto.UserName;
                }
                if (dto.Bio == null)
                {
                    utente.Bio = dto.Bio;
                }

                await _db.SaveChangesAsync();
                return true;
            }
      

        public async Task<UtenteProfiloDto?> GetProfilo(int? idUtente)
        {
            var profilo = await _db.Utenti
                .Where(u => u.IdUtente ==  idUtente)
                .Select(u => new UtenteProfiloDto
                {
                    IdUtente = u.IdUtente,
                    UserName = u.UserName,
                    Bio = u.Bio,
                    AvatarUrl = u.AvatarUrl,
                    DataDiRegistrazione = u.DataDiRegistrazione
                })
                .FirstOrDefaultAsync();

            return profilo;
        }

        public async Task<bool> DeleteProfilo(int id)
        {
            var profilo = await _db.Utenti.FindAsync(id);
            if (profilo == null) return false;

            _db.Utenti.Remove(profilo);
            await _db.SaveChangesAsync();
            return true;
        }

    }
}
