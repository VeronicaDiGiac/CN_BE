using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Discussioni su film e serie (anche sul singolo episodio). I messaggi sono Commenti
    // (CommentiService), così risposte, spoiler, "mi piace" e segnalazioni sono gli stessi.
    public class DiscussioniService
    {
        private readonly CinemaContext _db;
        private readonly NotificheService _notifiche;

        public DiscussioniService(CinemaContext db, NotificheService notifiche)
        {
            _db = db;
            _notifiche = notifiche;
        }

        public Task<Discussione?> GetDiscussioneById(int id) => _db.Discussioni.FindAsync(id).AsTask();

        public Task<bool> FilmEsiste(int idFilm) => _db.FilmApprovatoEsiste(idFilm);

        public Task<bool> ESerie(int idFilm) => _db.Film.AnyAsync(f => f.IdFilm == idFilm && f.Tipo == TipiTitolo.Serie);

        // Discussioni di un film. Con stagione ed episodio solo quelle di quell'episodio;
        // senza, tutte (generali e sugli episodi).
        public async Task<List<DiscussioneDto>> GetDiscussioni(int idFilm, int? stagione, int? episodio, string? ordinamento, int? idUtenteAutenticato)
        {
            var query = Proietta(
                _db.Discussioni
                    .Where(d => d.IdFilm == idFilm && !d.FiloEpisodio)
                    .Where(d => stagione == null || (d.Stagione == stagione && d.Episodio == episodio)),
                idUtenteAutenticato);

            query = ordinamento == OrdinamentiDiscussioni.Apprezzate
                ? query.OrderByDescending(d => d.NumeroMiPiace).ThenByDescending(d => d.NumeroCommenti).ThenByDescending(d => d.DataUltimaAttivita)
                : query.OrderByDescending(d => d.DataUltimaAttivita);
            return await query.ToListAsync();
        }

        public Task<DiscussioneDto?> GetDiscussione(int id, int? idUtenteAutenticato) =>
            Proietta(_db.Discussioni.Where(d => d.IdDiscussione == id), idUtenteAutenticato).FirstOrDefaultAsync();

        private IQueryable<DiscussioneDto> Proietta(IQueryable<Discussione> discussioni, int? idUtente) =>
            discussioni.Select(d => new DiscussioneDto
            {
                IdDiscussione = d.IdDiscussione,
                IdFilm = d.IdFilm,
                IdUtente = d.IdUtente,
                UserName = d.Utente!.UserName,
                AutoreAdmin = d.Utente.Ruolo == Ruoli.Admin,
                AvatarUrl = d.Utente.AvatarUrl,
                Titolo = d.Titolo,
                Contenuto = d.Contenuto,
                Spoiler = d.Spoiler,
                Stagione = d.Stagione,
                Episodio = d.Episodio,
                // All'autore non si nasconde quello che ha scritto lui
                EpisodioNonVisto = d.Stagione != null && d.IdUtente != idUtente &&
                    (idUtente == null || !_db.EpisodiVisti.Any(v => v.IdUtente == idUtente && v.IdFilm == d.IdFilm
                                                                   && v.Stagione == d.Stagione && v.Episodio == d.Episodio)),
                DataCreazione = d.DataCreazione,
                DataUltimaAttivita = d.DataUltimaAttivita,
                NumeroCommenti = d.Commenti!.Count(),
                FiloEpisodio = d.FiloEpisodio,
                NumeroMiPiace = d.MiPiace!.Count(),
                MiPiaceUtente = idUtente != null && d.MiPiace!.Any(m => m.IdUtente == idUtente)
            });

        // ---- Commenti di un episodio ----

        // Il filo dei commenti di un episodio, null se nessuno ha ancora commentato
        public Task<DiscussioneDto?> GetFiloEpisodio(int idFilm, int stagione, int episodio, int? idUtenteAutenticato) =>
            Proietta(_db.Discussioni.Where(d => d.FiloEpisodio && d.IdFilm == idFilm && d.Stagione == stagione && d.Episodio == episodio),
                idUtenteAutenticato).FirstOrDefaultAsync();

        // Il filo di un episodio, creato se non c'è ancora (chi commenta per primo ne è il "proprietario" tecnico,
        // che non si vede e non dà permessi). Ritorna l'id della discussione.
        public async Task<int> FiloEpisodio(int idFilm, int stagione, int episodio, int idUtente)
        {
            var esistente = await IdFilo(idFilm, stagione, episodio);
            if (esistente != null) return esistente.Value;

            var adesso = DateTime.Now;
            var filo = new Discussione
            {
                IdFilm = idFilm,
                IdUtente = idUtente,
                Titolo = $"Commenti all'episodio S{stagione} E{episodio}",
                Stagione = stagione,
                Episodio = episodio,
                FiloEpisodio = true,
                DataCreazione = adesso,
                DataUltimaAttivita = adesso
            };
            _db.Discussioni.Add(filo);
            try
            {
                await _db.SaveChangesAsync();
                return filo.IdDiscussione;
            }
            catch (DbUpdateException)
            {
                // Due primi commenti nello stesso momento: l'indice UX_Discussioni_FiloEpisodio ha tenuto l'altro
                _db.Entry(filo).State = EntityState.Detached;
                return (await IdFilo(idFilm, stagione, episodio))!.Value;
            }
        }

        private Task<int?> IdFilo(int idFilm, int stagione, int episodio) =>
            _db.Discussioni
                .Where(d => d.FiloEpisodio && d.IdFilm == idFilm && d.Stagione == stagione && d.Episodio == episodio)
                .Select(d => (int?)d.IdDiscussione)
                .FirstOrDefaultAsync();

        public async Task<int> Crea(SalvaDiscussioneDto dto, int idUtente)
        {
            var adesso = DateTime.Now;
            var discussione = new Discussione
            {
                IdFilm = dto.IdFilm,
                IdUtente = idUtente,
                Titolo = dto.Titolo.Trim(),
                Contenuto = string.IsNullOrWhiteSpace(dto.Contenuto) ? null : dto.Contenuto.Trim(),
                Spoiler = dto.Spoiler,
                Stagione = dto.Stagione,
                Episodio = dto.Episodio,
                DataCreazione = adesso,
                DataUltimaAttivita = adesso
            };
            _db.Discussioni.Add(discussione);
            await _db.SaveChangesAsync();
            return discussione.IdDiscussione;
        }

        // Si modificano solo titolo, testo e spoiler: film ed episodio restano quelli
        public async Task Aggiorna(Discussione discussione, SalvaDiscussioneDto dto)
        {
            discussione.Titolo = dto.Titolo.Trim();
            discussione.Contenuto = string.IsNullOrWhiteSpace(dto.Contenuto) ? null : dto.Contenuto.Trim();
            discussione.Spoiler = dto.Spoiler;
            await _db.SaveChangesAsync();
        }

        // Con tutti i messaggi, i "mi piace", le notifiche e le segnalazioni (vedi EliminazioneHelper)
        public async Task Elimina(int id)
        {
            using var transazione = await _db.Database.BeginTransactionAsync();
            await EliminazioneHelper.EliminaDiscussioni(_db, new List<int> { id });
            await transazione.CommitAsync();
        }

        public async Task MettiMiPiace(Discussione discussione, int idUtente)
        {
            if (await _db.MiPiaceDiscussioni.AnyAsync(m => m.IdDiscussione == discussione.IdDiscussione && m.IdUtente == idUtente)) return;
            _db.MiPiaceDiscussioni.Add(new MiPiaceDiscussione { IdDiscussione = discussione.IdDiscussione, IdUtente = idUtente, DataMiPiace = DateTime.Now });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Due clic contemporanei: la chiave primaria ha già evitato il doppione
                return;
            }
            await _notifiche.Notifica(discussione.IdUtente, idUtente, TipiNotifica.MiPiaceDiscussione,
                discussione.IdFilm, idDiscussione: discussione.IdDiscussione);
        }

        public async Task TogliMiPiace(Discussione discussione, int idUtente)
        {
            var tolti = await _db.MiPiaceDiscussioni
                .Where(m => m.IdDiscussione == discussione.IdDiscussione && m.IdUtente == idUtente)
                .ExecuteDeleteAsync();
            if (tolti > 0)
                await _notifiche.Annulla(discussione.IdUtente, idUtente, TipiNotifica.MiPiaceDiscussione, idDiscussione: discussione.IdDiscussione);
        }
    }
}
