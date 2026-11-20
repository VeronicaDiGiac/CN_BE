using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // "Sto guardando": lo accende l'utente dalla scheda (mai automatico). Dura quanto il film
    // (o un episodio, per le serie) più un margine; finisce prima se lo spegne, se ne accende
    // un altro o se segna il film come visto. Lo vedono solo lui e chi lo segue.
    public class StoGuardandoService
    {
        // Durata usata quando TMDB non la conosce, e margine per pause e titoli di coda
        public const int MinutiFilmPredefiniti = 120;
        public const int MinutiEpisodioPredefiniti = 50;
        public const int MinutiMargine = 30;

        private readonly CinemaContext _db;
        private readonly NotificheService _notifiche;

        public StoGuardandoService(CinemaContext db, NotificheService notifiche)
        {
            _db = db;
            _notifiche = notifiche;
        }

        // null se il film non esiste o non è approvato
        public async Task<StaGuardandoDto?> Inizia(int idUtente, int idFilm)
        {
            var film = await _db.Film
                .Where(f => f.IdFilm == idFilm && f.Stato == StatiFilm.Approvato)
                .Select(f => new { f.Tipo, f.DurataMinuti })
                .FirstOrDefaultAsync();
            if (film == null) return null;

            var minuti = (film.DurataMinuti ?? (film.Tipo == TipiTitolo.Serie ? MinutiEpisodioPredefiniti : MinutiFilmPredefiniti)) + MinutiMargine;
            var ora = DateTime.Now;
            var esistente = await _db.StoGuardando.FirstOrDefaultAsync(s => s.IdUtente == idUtente);
            // Già acceso sullo stesso titolo: si allunga la durata, senza avvisare di nuovo
            var stessoTitolo = esistente != null && esistente.IdFilm == idFilm && esistente.Fine > ora;
            if (esistente == null)
                _db.StoGuardando.Add(new StoGuardando { IdUtente = idUtente, IdFilm = idFilm, Inizio = ora, Fine = ora.AddMinutes(minuti) });
            else
            {
                esistente.IdFilm = idFilm;
                if (!stessoTitolo) esistente.Inizio = ora;
                esistente.Fine = ora.AddMinutes(minuti);
            }
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Due tocchi insieme: la riga l'ha già creata l'altra richiesta
                return await DiUtente(idUtente);
            }

            if (!stessoTitolo)
                await _notifiche.NotificaSeguaci(idUtente, TipiNotifica.SeguitoGuarda, idFilm);
            return await DiUtente(idUtente);
        }

        public Task Smetti(int idUtente) =>
            _db.StoGuardando.Where(s => s.IdUtente == idUtente).ExecuteDeleteAsync();

        // Segnato come visto: non lo sta più guardando
        public static Task FinitoDiGuardare(CinemaContext db, int idUtente, int idFilm) =>
            db.StoGuardando.Where(s => s.IdUtente == idUtente && s.IdFilm == idFilm).ExecuteDeleteAsync();

        // Cosa sta guardando un utente, se lo può vedere chi chiede (se stesso o chi lo segue)
        public async Task<StaGuardandoDto?> PerProfilo(int idUtente, int? idChiede)
        {
            if (idChiede == null) return null;
            if (idChiede != idUtente && !await _db.Follow.AnyAsync(f => f.FollowerId == idChiede && f.FollowingId == idUtente))
                return null;
            return await DiUtente(idUtente);
        }

        // Le persone seguite che stanno guardando qualcosa adesso, le più recenti prima
        public Task<List<StaGuardandoDto>> Seguiti(int idUtente)
        {
            var seguiti = _db.Follow.Where(f => f.FollowerId == idUtente).Select(f => f.FollowingId);
            return Attivi(_db.StoGuardando.Where(s => seguiti.Contains(s.IdUtente)))
                .OrderByDescending(s => s.Inizio)
                .Take(30)
                .ToListAsync();
        }

        private Task<StaGuardandoDto?> DiUtente(int idUtente) =>
            Attivi(_db.StoGuardando.Where(s => s.IdUtente == idUtente)).FirstOrDefaultAsync();

        private static IQueryable<StaGuardandoDto> Attivi(IQueryable<StoGuardando> righe)
        {
            var ora = DateTime.Now;
            return righe
                .Where(s => s.Fine > ora)
                .Select(s => new StaGuardandoDto
                {
                    IdUtente = s.IdUtente,
                    UserName = s.Utente!.UserName,
                    AvatarUrl = s.Utente.AvatarUrl,
                    IdFilm = s.IdFilm,
                    TitoloFilm = s.Film!.Titolo,
                    ImmagineUrl = s.Film.ImmagineUrl,
                    Tipo = s.Film.Tipo,
                    Inizio = s.Inizio,
                    Fine = s.Fine
                });
        }
    }
}
