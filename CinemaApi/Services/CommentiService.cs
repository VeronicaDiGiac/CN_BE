using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Commenti sotto una recensione oppure messaggi di una discussione (esattamente uno dei due):
    // stesse regole per risposte, spoiler, "mi piace", notifiche ed eliminazione.
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

        // Commenti principali con le loro risposte. idUtenteAutenticato serve a MiPiaceUtente.
        public async Task<List<CommentoDto>> GetCommenti(int? idRecensione, int? idUtente, int? idDiscussione = null, int? idUtenteAutenticato = null)
        {
            return await _db.Commenti
                .Where(c => idRecensione == null || c.IdRecensione == idRecensione)
                .Where(c => idDiscussione == null || c.IdDiscussione == idDiscussione)
                .Where(c => c.IdCommentoPadre == null)
                .Where(c => idUtente == null || c.IdUtente == idUtente)
                .OrderBy(c => c.DataCommento)
                .Select(c => new CommentoDto
                {
                    IdCommento = c.IdCommento,
                    IdRecensione = c.IdRecensione,
                    IdDiscussione = c.IdDiscussione,
                    UserName = c.Utente != null ? c.Utente.UserName : null,
                    AutoreAdmin = c.Utente != null && c.Utente.Ruolo == Ruoli.Admin,
                    IdUtente = c.IdUtente,
                    Contenuto = c.Contenuto,
                    DataCommento = c.DataCommento,
                    IdCommentoPadre = c.IdCommentoPadre,
                    Spoiler = c.Spoiler,
                    NumeroMiPiace = c.MiPiace!.Count(),
                    MiPiaceUtente = idUtenteAutenticato != null && c.MiPiace!.Any(m => m.IdUtente == idUtenteAutenticato),
                    Risposte = c.Risposte != null ? c.Risposte.OrderBy(r => r.DataCommento).Select(r => new CommentoDto
                    {
                        IdCommento = r.IdCommento,
                        IdRecensione = r.IdRecensione,
                        IdDiscussione = r.IdDiscussione,
                        IdUtente = r.IdUtente,
                        UserName = r.Utente != null ? r.Utente.UserName : null,
                        AutoreAdmin = r.Utente != null && r.Utente.Ruolo == Ruoli.Admin,
                        Contenuto = r.Contenuto,
                        DataCommento = r.DataCommento,
                        IdCommentoPadre = r.IdCommentoPadre,
                        Spoiler = r.Spoiler,
                        NumeroMiPiace = r.MiPiace!.Count(),
                        MiPiaceUtente = idUtenteAutenticato != null && r.MiPiace!.Any(m => m.IdUtente == idUtenteAutenticato)
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
                IdDiscussione = dto.IdDiscussione,
                Contenuto = dto.Contenuto,
                DataCommento = DateTime.Now,
                IdCommentoPadre = dto.IdCommentoPadre,
                Spoiler = dto.Spoiler
            };
            _db.Commenti.Add(commento);
            await _db.SaveChangesAsync();

            if (commento.IdDiscussione != null)
            {
                // Un messaggio nuovo riporta la discussione in cima alle "più recenti"
                await _db.Discussioni
                    .Where(d => d.IdDiscussione == commento.IdDiscussione)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.DataUltimaAttivita, commento.DataCommento!.Value));
            }
            await NotificaCommento(commento);

            return new CommentoDto
            {
                IdCommento = commento.IdCommento,
                IdRecensione = commento.IdRecensione,
                IdDiscussione = commento.IdDiscussione,
                IdUtente = commento.IdUtente,
                UserName = await _db.Utenti.Where(u => u.IdUtente == idUtente).Select(u => u.UserName).FirstOrDefaultAsync(),
                AutoreAdmin = await _db.Utenti.AnyAsync(u => u.IdUtente == idUtente && u.Ruolo == Ruoli.Admin),
                Contenuto = commento.Contenuto,
                DataCommento = commento.DataCommento,
                IdCommentoPadre = commento.IdCommentoPadre,
                Spoiler = commento.Spoiler,
                Risposte = new List<CommentoDto>()
            };
        }

        // Dove sta un commento: autore e film del contenitore (recensione o discussione).
        // Proiezione e non FindAsync: caricare le entità le collegherebbe al commento appena creato
        private async Task<(int IdAutore, int IdFilm)?> Contenitore(Commento commento)
        {
            if (commento.IdRecensione != null)
            {
                var r = await _db.Recensioni.Where(x => x.IdRecensione == commento.IdRecensione)
                    .Select(x => new { x.IdUtente, x.IdFilm }).FirstOrDefaultAsync();
                return r == null ? null : (r.IdUtente, r.IdFilm);
            }
            var d = await _db.Discussioni.Where(x => x.IdDiscussione == commento.IdDiscussione)
                .Select(x => new { x.IdUtente, x.IdFilm }).FirstOrDefaultAsync();
            return d == null ? null : (d.IdUtente, d.IdFilm);
        }

        // Avvisa l'autore della recensione (o della discussione) e, per le risposte, l'autore del
        // commento a cui si risponde. Se sono la stessa persona riceve una sola notifica (la risposta).
        private async Task NotificaCommento(Commento commento)
        {
            var contenitore = await Contenitore(commento);
            if (contenitore == null) return;
            var (idAutore, idFilm) = contenitore.Value;

            int? autorePadre = null;
            if (commento.IdCommentoPadre != null)
            {
                autorePadre = await _db.Commenti
                    .Where(c => c.IdCommento == commento.IdCommentoPadre)
                    .Select(c => (int?)c.IdUtente)
                    .FirstOrDefaultAsync();
                if (autorePadre != null)
                    await _notifiche.Notifica(autorePadre.Value, commento.IdUtente, TipiNotifica.RispostaCommento,
                        idFilm, commento.IdRecensione, commento.IdCommento, commento.IdDiscussione);
            }

            if (idAutore != autorePadre)
                await _notifiche.Notifica(idAutore, commento.IdUtente,
                    commento.IdDiscussione != null ? TipiNotifica.CommentoDiscussione : TipiNotifica.CommentoRecensione,
                    idFilm, commento.IdRecensione, commento.IdCommento, commento.IdDiscussione);
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

        public Task<bool> DiscussioneEsiste(int idDiscussione) =>
            _db.Discussioni.AnyAsync(d => d.IdDiscussione == idDiscussione);

        // Una risposta deve stare sotto un commento esistente dello STESSO contenitore
        public async Task<bool> CommentoPadreValido(int idCommentoPadre, int? idRecensione, int? idDiscussione)
        {
            return await _db.Commenti.AnyAsync(c => c.IdCommento == idCommentoPadre
                                                    && c.IdRecensione == idRecensione && c.IdDiscussione == idDiscussione);
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

        // ---- "Mi piace" ----

        public async Task MettiMiPiace(Commento commento, int idUtente)
        {
            if (await _db.MiPiaceCommenti.AnyAsync(m => m.IdCommento == commento.IdCommento && m.IdUtente == idUtente)) return;
            _db.MiPiaceCommenti.Add(new MiPiaceCommento { IdCommento = commento.IdCommento, IdUtente = idUtente, DataMiPiace = DateTime.Now });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Due clic contemporanei: la chiave primaria ha già evitato il doppione
                return;
            }

            var contenitore = await Contenitore(commento);
            await _notifiche.Notifica(commento.IdUtente, idUtente, TipiNotifica.MiPiaceCommento,
                contenitore?.IdFilm, commento.IdRecensione, commento.IdCommento, commento.IdDiscussione);
        }

        public async Task TogliMiPiace(Commento commento, int idUtente)
        {
            var tolti = await _db.MiPiaceCommenti
                .Where(m => m.IdCommento == commento.IdCommento && m.IdUtente == idUtente)
                .ExecuteDeleteAsync();
            if (tolti > 0)
                await _notifiche.Annulla(commento.IdUtente, idUtente, TipiNotifica.MiPiaceCommento, idCommento: commento.IdCommento);
        }
    }
}
