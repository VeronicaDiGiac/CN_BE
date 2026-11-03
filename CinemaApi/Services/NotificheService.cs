using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Crea e legge le notifiche. Gli altri service la chiamano dopo l'azione che le genera
    // (follow, "mi piace", commento): un solo punto in cui stanno le regole.
    public class NotificheService
    {
        private readonly CinemaContext _db;

        public NotificheService(CinemaContext db) { _db = db; }

        // Non si notifica a se stessi (es. rispondere a un commento sotto la propria recensione)
        public async Task Notifica(int idDestinatario, int idMittente, string tipo,
            int? idFilm = null, int? idRecensione = null, int? idCommento = null)
        {
            if (idDestinatario == idMittente) return;

            _db.Notifiche.Add(new Notifica
            {
                IdDestinatario = idDestinatario,
                IdMittente = idMittente,
                Tipo = tipo,
                IdFilm = idFilm,
                IdRecensione = idRecensione,
                IdCommento = idCommento,
                DataCreazione = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        // Toglie una notifica non ancora letta quando l'azione viene annullata
        // (es. "mi piace" tolto subito dopo): evita notifiche di cose che non esistono più
        public Task Annulla(int idDestinatario, int idMittente, string tipo, int? idRecensione = null) =>
            _db.Notifiche
                .Where(n => n.IdDestinatario == idDestinatario && n.IdMittente == idMittente && n.Tipo == tipo
                            && n.IdRecensione == idRecensione && !n.Letta)
                .ExecuteDeleteAsync();

        public async Task<PagedResultDto<NotificaDto>> GetNotifiche(int idUtente, int pageNumber, int pageSize)
        {
            var query = _db.Notifiche.Where(n => n.IdDestinatario == idUtente);
            var totale = await query.CountAsync();

            var voci = await query
                .OrderByDescending(n => n.DataCreazione)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificaDto
                {
                    IdNotifica = n.IdNotifica,
                    Tipo = n.Tipo,
                    Letta = n.Letta,
                    DataCreazione = n.DataCreazione,
                    IdMittente = n.IdMittente,
                    UserNameMittente = n.Mittente != null ? n.Mittente.UserName : null,
                    AvatarMittente = n.Mittente != null ? n.Mittente.AvatarUrl : null,
                    IdFilm = n.IdFilm,
                    TitoloFilm = _db.Film.Where(f => f.IdFilm == n.IdFilm).Select(f => f.Titolo).FirstOrDefault(),
                    IdRecensione = n.IdRecensione,
                    IdCommento = n.IdCommento,
                    Stagione = n.Stagione,
                    Episodio = n.Episodio
                })
                .ToListAsync();

            return new PagedResultDto<NotificaDto>
            {
                TotalCount = totale,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = voci
            };
        }

        public Task<int> ContaNonLette(int idUtente) =>
            _db.Notifiche.CountAsync(n => n.IdDestinatario == idUtente && !n.Letta);

        public Task SegnaTutteLette(int idUtente) =>
            _db.Notifiche
                .Where(n => n.IdDestinatario == idUtente && !n.Letta)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.Letta, true));
    }
}
