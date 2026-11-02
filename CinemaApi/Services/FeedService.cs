using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Feed delle attività delle persone seguite. Non ha una tabella sua: unisce al volo
    // visioni e recensioni con una sola query (UNION ALL), ordinata e paginata dal DB.
    // Per aggiungere un nuovo tipo di attività basta un'altra Select con la stessa forma.
    public class FeedService
    {
        private readonly CinemaContext _db;

        public FeedService(CinemaContext db) { _db = db; }

        public async Task<PagedResultDto<FeedVoceDto>> GetFeed(int idUtente, int pageNumber, int pageSize)
        {
            var seguiti = _db.Follow.Where(f => f.FollowerId == idUtente).Select(f => f.FollowingId);

            var visioni = _db.Visioni
                .Where(v => seguiti.Contains(v.IdUtente))
                .Select(v => new FeedVoceDto
                {
                    Tipo = TipiAttivita.Visione,
                    Data = v.DataCreazione,
                    IdUtente = v.IdUtente,
                    UserName = v.Utente!.UserName,
                    AvatarUrl = v.Utente.AvatarUrl,
                    IdFilm = v.IdFilm,
                    TitoloFilm = v.Film!.Titolo,
                    ImmagineUrl = v.Film.ImmagineUrl,
                    IdRecensione = null,
                    Voto = null,
                    Testo = null,
                    Spoiler = false
                });

            var recensioni = _db.Recensioni
                .Where(r => seguiti.Contains(r.IdUtente) && r.DataRecensione != null)
                .Select(r => new FeedVoceDto
                {
                    Tipo = TipiAttivita.Recensione,
                    Data = r.DataRecensione!.Value,
                    IdUtente = r.IdUtente,
                    UserName = r.Utente!.UserName,
                    AvatarUrl = r.Utente.AvatarUrl,
                    IdFilm = r.IdFilm,
                    TitoloFilm = r.Film!.Titolo,
                    ImmagineUrl = r.Film.ImmagineUrl,
                    IdRecensione = r.IdRecensione,
                    Voto = r.Voto,
                    Testo = r.Contenuto,
                    Spoiler = r.Spoiler
                });

            var tutte = visioni.Concat(recensioni);
            var totale = await tutte.CountAsync();
            var voci = await tutte
                .OrderByDescending(v => v.Data)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<FeedVoceDto>
            {
                TotalCount = totale,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = voci
            };
        }
    }
}
