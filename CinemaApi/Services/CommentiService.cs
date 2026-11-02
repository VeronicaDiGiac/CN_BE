using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    public class CommentiService
    {
        private readonly CinemaContext _db;
        private readonly NotificheService _notifiche;

        public CommentiService(CinemaContext db, NotificheService notifiche)
        {
            _db = db;
            _notifiche = notifiche;
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
                    IdRecensione = c.IdRecensione,
                    UserName = c.Utente != null ? c.Utente.UserName : null,
                    IdUtente = c.IdUtente,
                    Contenuto = c.Contenuto,
                    DataCommento = c.DataCommento,
                    IdCommentoPadre = c.IdCommentoPadre,
                    Spoiler = c.Spoiler,
                    Risposte = c.Risposte != null ? c.Risposte.Select(r => new CommentoDto
                    {
                        IdCommento = r.IdCommento,
                        IdRecensione = r.IdRecensione,
                        //Avevo scritto c.IdUtente
                        IdUtente = r.IdUtente,
                        UserName = r.Utente != null ? r.Utente.UserName : null,
                        Contenuto = r.Contenuto,
                        DataCommento = r.DataCommento,
                        IdCommentoPadre = r.IdCommentoPadre,
                        Spoiler = r.Spoiler
                    }).ToList() : null
                })
                .ToListAsync();
        }

        public async Task<CommentoDto> CreateCommento(CreateCommentoDto dto, int idUtente)
        {
            var commento = new Commento
            {
                IdUtente = idUtente,
                IdRecensione = dto.IdRecensione,
                Contenuto = dto.Contenuto,
                DataCommento = DateTime.Now,
                IdCommentoPadre = dto.IdCommentoPadre,
                Spoiler = dto.Spoiler
            };
            _db.Commenti.Add(commento);
            await _db.SaveChangesAsync();
            await NotificaCommento(commento);
            return new CommentoDto
            {
                IdCommento = commento.IdCommento,
                IdRecensione = commento.IdRecensione,
                IdUtente = commento.IdUtente,
                UserName = await _db.Utenti.Where(u => u.IdUtente == idUtente).Select(u => u.UserName).FirstOrDefaultAsync(),
                Contenuto = commento.Contenuto,
                DataCommento = commento.DataCommento,
                IdCommentoPadre = commento.IdCommentoPadre,
                Spoiler = commento.Spoiler,
                Risposte = new List<CommentoDto>()
            };
        }

        // Avvisa l'autore della recensione e, per le risposte, l'autore del commento a cui si risponde.
        // Se sono la stessa persona riceve una sola notifica (quella della risposta).
        private async Task NotificaCommento(Commento commento)
        {
            // Proiezione e non FindAsync: caricare l'entità Recensione la collegherebbe al commento
            // appena creato (che il controller restituisce) e il JSON andrebbe in ciclo
            var recensione = await _db.Recensioni
                .Where(r => r.IdRecensione == commento.IdRecensione)
                .Select(r => new { r.IdRecensione, r.IdUtente, r.IdFilm })
                .FirstOrDefaultAsync();
            if (recensione == null) return;

            int? autorePadre = null;
            if (commento.IdCommentoPadre != null)
            {
                autorePadre = await _db.Commenti
                    .Where(c => c.IdCommento == commento.IdCommentoPadre)
                    .Select(c => (int?)c.IdUtente)
                    .FirstOrDefaultAsync();
                if (autorePadre != null)
                    await _notifiche.Notifica(autorePadre.Value, commento.IdUtente, TipiNotifica.RispostaCommento,
                        recensione.IdFilm, recensione.IdRecensione, commento.IdCommento);
            }

            if (recensione.IdUtente != autorePadre)
                await _notifiche.Notifica(recensione.IdUtente, commento.IdUtente, TipiNotifica.CommentoRecensione,
                    recensione.IdFilm, recensione.IdRecensione, commento.IdCommento);
        }

        public async Task<bool> UpdateCommento(int id, CreateCommentoDto dto)
        {
            var commento = await _db.Commenti.FindAsync(id);
            if (commento == null) return false;

            commento.Contenuto = dto.Contenuto;
            commento.Spoiler = dto.Spoiler;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RecensioneEsiste(int idRecensione)
        {
            return await _db.Recensioni.AnyAsync(r => r.IdRecensione == idRecensione);
        }

        // Una risposta deve stare sotto un commento esistente della STESSA recensione.
        public async Task<bool> CommentoPadreValido(int idCommentoPadre, int idRecensione)
        {
            return await _db.Commenti.AnyAsync(c => c.IdCommento == idCommentoPadre && c.IdRecensione == idRecensione);
        }

        // Elimina il commento insieme a tutte le sue risposte (vedi EliminazioneHelper).
        public async Task<bool> DeleteCommento(int id)
        {
            var commento = await _db.Commenti.FindAsync(id);
            if (commento == null) return false;

            using var transazione = await _db.Database.BeginTransactionAsync();
            await EliminazioneHelper.EliminaCommenti(_db, new List<int> { id });
            await transazione.CommitAsync();
            return true;
        }
    }
}