using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    public class UtentiService
    {
        private readonly CinemaContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UtentiService(CinemaContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<bool> UpdateProfilo(int id, UpdateProfiloDto dto)
        {
            var utente = await _db.Utenti.FindAsync(id);
            if (utente == null) return false;

            // Aggiorna SOLO i campi effettivamente forniti (!= null),
            // così un aggiornamento parziale non azzera i campi non inviati.
            if (dto.UserName != null)
            {
                utente.UserName = dto.UserName;
            }
            if (dto.Bio != null)
            {
                utente.Bio = dto.Bio;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<UtenteProfiloDto?> GetProfilo(int? idUtente)
        {
            var profilo = await _db.Utenti
                .Where(u => u.IdUtente == idUtente)
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

        public async Task<string?> SaveAvatar(int id, IFormFile file)
        {
            var utente = await _db.Utenti.FindAsync(id);
            if (utente == null) return null;

            var estensione = Path.GetExtension(file.FileName);
            // Genera un nome univoco tipo: a3f29c1e-8b4d-4f2a-9c3e-1d5f7a8b9c0d.jpg
            var nomeFileUnivoco = $"{Guid.NewGuid()}{estensione}";

            // Percorso fisico della cartella, es: C:\...\wwwroot\uploads\avatar
            var cartellaUpload = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatar");
            var percorsoCompleto = Path.Combine(cartellaUpload, nomeFileUnivoco);

            // Salva il file sul disco fisico
            using (var stream = new FileStream(percorsoCompleto, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // URL relativo pubblico (corretto, senza refusi)
            var urlRelativo = $"/uploads/avatar/{nomeFileUnivoco}";
            utente.AvatarUrl = urlRelativo;
            await _db.SaveChangesAsync();

            return urlRelativo;
        }
    }
}