using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    public class CommentiService
    {
        private readonly CinemaContext _db;

        public CommentiService(CinemaContext db)
        {
            _db = db;
        }

        public async Task<Commento?> GetCommentoById(int id)
        {
            return await _db.Commenti.FindAsync(id);
        }

        public async Task<List<CommentoDto>> GetCommenti(int? idRecensione, int? idUtente)
        {
            return await _db.Commenti
                .Include(c => c.Utente)
                .Include(c => c.Risposte)
                .Where(c => idRecensione == null || c.IdRecensione == idRecensione)
                .Where(c => c.IdCommentoPadre == null)
                //Errore controllo sulla colonna e non sul parametro
                //.Where(c => c.IdUtente == null || c.IdUtente== idUtente)
                .Where(c => idUtente == null || c.IdUtente == idUtente)
                .Select(c => new CommentoDto
                {
                    IdCommento = c.IdCommento,
                    UserName = c.Utente != null ? c.Utente.UserName : null,
                    IdUtente = c.IdUtente,
                    Contenuto = c.Contenuto,
                    DataCommento = c.DataCommento,
                    IdCommentoPadre = c.IdCommentoPadre,
                    Risposte = c.Risposte != null ? c.Risposte.Select(r => new CommentoDto
                    {
                        IdCommento = r.IdCommento,
                        IdUtente = c.IdUtente,
                        UserName = r.Utente != null ? r.Utente.UserName : null,
                        Contenuto = r.Contenuto,
                        DataCommento = r.DataCommento
                    }).ToList() : null
                })
                .ToListAsync();
        }

        public async Task<Commento> CreateCommento(CreateCommentoDto dto)
        {
            var commento = new Commento
            {
                //IdUtente = dto.IdUtente,
                IdRecensione = dto.IdRecensione,
                Contenuto = dto.Contenuto,
                DataCommento = DateTime.Now,
                IdCommentoPadre = dto.IdCommentoPadre
            };
            _db.Commenti.Add(commento);
            await _db.SaveChangesAsync();
            return commento;
        }

        public async Task<bool> UpdateCommento(int id, CreateCommentoDto dto)
        {
            var commento = await _db.Commenti.FindAsync(id);
            if (commento == null) return false;

            commento.Contenuto = dto.Contenuto;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCommento(int id)
        {
            var commento = await _db.Commenti.FindAsync(id);
            if (commento == null) return false;

            _db.Commenti.Remove(commento);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}