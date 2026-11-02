using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    public class FollowService
    {
        private readonly CinemaContext _db;
        private readonly NotificheService _notifiche;

        public FollowService(CinemaContext db, NotificheService notifiche)
        {
            _db = db;
            _notifiche = notifiche;
        }

        public async Task<List<FollowDto>> GetFollowing(int idUtente)
        {
            return await _db.Follow
                .Where(f => f.FollowerId == idUtente)
                .Select(f => new FollowDto
                {
                    IdFollow = f.IdFollow,
                    IdUtente = f.FollowingId,
                    UserName = f.Following != null ? f.Following.UserName : null,    
                    AvatarUrl = f.Following != null ? f.Following.AvatarUrl : null,  
                    DataFollow = f.DataFollow
                })
                .ToListAsync();
        }

        public async Task<List<FollowDto>> GetFollowers(int idUtente)
        {
            return await _db.Follow
                .Where(f => f.FollowingId == idUtente)
                .Select(f => new FollowDto
                {
                    IdFollow = f.IdFollow,
                    IdUtente = f.FollowerId,
                    UserName = f.Follower != null ? f.Follower.UserName : null,
                    AvatarUrl = f.Follower != null ? f.Follower.AvatarUrl : null,
                    DataFollow = f.DataFollow
                })
                .ToListAsync();
        }

        public async Task<bool> UtenteEsiste(int idUtente)
        {
            return await _db.Utenti.AnyAsync(u => u.IdUtente == idUtente);
        }

        public async Task<FollowDto?> CreateFollow(CreateFollowDto dto, int idFollower)
        {
            var esistente = await _db.Follow
            .FirstOrDefaultAsync(f => f.FollowerId == idFollower && f.FollowingId == dto.FollowingId);
            if (esistente != null) return null;
            var follow = new Follow
            {
                FollowerId = idFollower,
                FollowingId = dto.FollowingId,
                DataFollow = DateTime.Now
            };
            _db.Follow.Add(follow);
            await _db.SaveChangesAsync();
            await _notifiche.Notifica(dto.FollowingId, idFollower, TipiNotifica.NuovoFollower);
            // Come in GetFollowing: l'utente mostrato è quello appena seguito
            return await _db.Follow
                .Where(f => f.IdFollow == follow.IdFollow)
                .Select(f => new FollowDto
                {
                    IdFollow = f.IdFollow,
                    IdUtente = f.FollowingId,
                    UserName = f.Following != null ? f.Following.UserName : null,
                    AvatarUrl = f.Following != null ? f.Following.AvatarUrl : null,
                    DataFollow = f.DataFollow
                })
                .FirstAsync();
        }


        public async Task <bool> DeleteFollow (int followerId, int followingId)
        {
            var follow = await _db.Follow.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
            if (follow == null) return false;
            _db.Follow.Remove(follow);
            await _db.SaveChangesAsync();
            await _notifiche.Annulla(followingId, followerId, TipiNotifica.NuovoFollower);
            return true;

        }
    }
}
