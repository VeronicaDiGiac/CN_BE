using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Stelle date ai film (VotiFilm) e alle recensioni degli altri (VotiRecensioni).
    // Il voto al film è indipendente dalla recensione: si vota anche senza scrivere.
    public class VotiService
    {
        private readonly CinemaContext _db;
        private readonly NotificheService _notifiche;

        public VotiService(CinemaContext db, NotificheService notifiche)
        {
            _db = db;
            _notifiche = notifiche;
        }

        public Task<bool> FilmEsiste(int idFilm) => _db.FilmApprovatoEsiste(idFilm);

        public Task<Recensione?> GetRecensione(int id) => _db.Recensioni.FirstOrDefaultAsync(r => r.IdRecensione == id);

        // Crea o cambia il voto: uno per utente e film
        public async Task<VotiDto> VotaFilm(int idFilm, int idUtente, int voto)
        {
            await ImpostaVotoFilm(_db, idFilm, idUtente, voto);
            await _notifiche.NotificaSeguaci(idUtente, TipiNotifica.SeguitoVoto, idFilm, voto: voto);
            return await RiepilogoFilm(idFilm, idUtente);
        }

        public async Task<VotiDto> TogliVotoFilm(int idFilm, int idUtente)
        {
            await _db.VotiFilm.Where(v => v.IdFilm == idFilm && v.IdUtente == idUtente).ExecuteDeleteAsync();
            return await RiepilogoFilm(idFilm, idUtente);
        }

        public async Task<VotiDto> VotaRecensione(int idRecensione, int idUtente, int voto)
        {
            var righe = await _db.VotiRecensioni
                .Where(v => v.IdRecensione == idRecensione && v.IdUtente == idUtente)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Voto, voto).SetProperty(v => v.DataVoto, DateTime.Now));
            if (righe == 0)
            {
                var nuovo = new VotoRecensione { IdRecensione = idRecensione, IdUtente = idUtente, Voto = voto, DataVoto = DateTime.Now };
                _db.VotiRecensioni.Add(nuovo);
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Due clic contemporanei: l'altro ha già inserito la riga, si aggiorna quella
                    _db.Entry(nuovo).State = EntityState.Detached;
                    await _db.VotiRecensioni
                        .Where(v => v.IdRecensione == idRecensione && v.IdUtente == idUtente)
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.Voto, voto));
                }
            }
            return await RiepilogoRecensione(idRecensione, idUtente);
        }

        public async Task<VotiDto> TogliVotoRecensione(int idRecensione, int idUtente)
        {
            await _db.VotiRecensioni.Where(v => v.IdRecensione == idRecensione && v.IdUtente == idUtente).ExecuteDeleteAsync();
            return await RiepilogoRecensione(idRecensione, idUtente);
        }

        // Usato anche da RecensioniService: scrivere una recensione con le stelle vota il film
        public static async Task ImpostaVotoFilm(CinemaContext db, int idFilm, int idUtente, int voto)
        {
            var righe = await db.VotiFilm
                .Where(v => v.IdFilm == idFilm && v.IdUtente == idUtente)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Voto, voto).SetProperty(v => v.DataVoto, DateTime.Now));
            if (righe > 0) return;

            var nuovo = new VotoFilm { IdFilm = idFilm, IdUtente = idUtente, Voto = voto, DataVoto = DateTime.Now };
            db.VotiFilm.Add(nuovo);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                db.Entry(nuovo).State = EntityState.Detached;
                await db.VotiFilm
                    .Where(v => v.IdFilm == idFilm && v.IdUtente == idUtente)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.Voto, voto));
            }
        }

        private Task<VotiDto> RiepilogoFilm(int idFilm, int idUtente) =>
            Riepilogo(_db.VotiFilm.Where(v => v.IdFilm == idFilm).Select(v => v.Voto),
                      _db.VotiFilm.Where(v => v.IdFilm == idFilm && v.IdUtente == idUtente).Select(v => (int?)v.Voto));

        private Task<VotiDto> RiepilogoRecensione(int idRecensione, int idUtente) =>
            Riepilogo(_db.VotiRecensioni.Where(v => v.IdRecensione == idRecensione).Select(v => v.Voto),
                      _db.VotiRecensioni.Where(v => v.IdRecensione == idRecensione && v.IdUtente == idUtente).Select(v => (int?)v.Voto));

        private static async Task<VotiDto> Riepilogo(IQueryable<int> voti, IQueryable<int?> votoUtente) => new VotiDto
        {
            VotoMedio = await voti.Select(v => (double?)v).AverageAsync(),
            NumeroVoti = await voti.CountAsync(),
            VotoUtente = await votoUtente.FirstOrDefaultAsync()
        };
    }
}
