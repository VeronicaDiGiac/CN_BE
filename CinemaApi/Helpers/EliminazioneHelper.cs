using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Helpers
{
    // Eliminazione "a cascata" fatta nel codice e non nel DB: SQL Server non
    // accetta ON DELETE CASCADE qui (Utenti -> Commenti si raggiunge per due strade,
    // direttamente e passando da Recensioni, e Commenti punta anche a se stessa).
    // Questi metodi usano ExecuteDeleteAsync, che scrive subito sul DB: chi li chiama
    // deve aprire una transazione, così se qualcosa fallisce non resta niente a metà.
    public static class EliminazioneHelper
    {
        // Elimina i commenti indicati, TUTTE le loro risposte (a qualsiasi livello)
        // e le segnalazioni che li riguardano.
        public static async Task EliminaCommenti(CinemaContext db, List<int> idCommenti)
        {
            // Raccolgo l'albero delle risposte livello per livello
            var tutti = new List<int>(idCommenti);
            var livello = idCommenti;
            while (livello.Any())
            {
                livello = await db.Commenti
                    .Where(c => c.IdCommentoPadre != null && livello.Contains(c.IdCommentoPadre.Value))
                    .Select(c => c.IdCommento)
                    .ToListAsync();
                tutti.AddRange(livello);
            }

            await db.Segnalazioni
                .Where(s => s.TipoContenuto == TipiContenuto.Commento && tutti.Contains(s.IdContenuto))
                .ExecuteDeleteAsync();

            await db.Notifiche
                .Where(n => n.IdCommento != null && tutti.Contains(n.IdCommento.Value))
                .ExecuteDeleteAsync();

            await db.MiPiaceCommenti
                .Where(m => tutti.Contains(m.IdCommento))
                .ExecuteDeleteAsync();

            // Prima stacco le risposte dal padre, altrimenti la FK Commenti -> Commenti
            // blocca l'eliminazione in base all'ordine delle righe.
            await db.Commenti
                .Where(c => tutti.Contains(c.IdCommento))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IdCommentoPadre, (int?)null));

            await db.Commenti
                .Where(c => tutti.Contains(c.IdCommento))
                .ExecuteDeleteAsync();
        }

        // Elimina le discussioni indicate con tutti i loro messaggi, "mi piace", notifiche e segnalazioni.
        public static async Task EliminaDiscussioni(CinemaContext db, List<int> idDiscussioni)
        {
            var idCommenti = await db.Commenti
                .Where(c => c.IdDiscussione != null && idDiscussioni.Contains(c.IdDiscussione.Value))
                .Select(c => c.IdCommento)
                .ToListAsync();
            await EliminaCommenti(db, idCommenti);

            await db.Segnalazioni
                .Where(s => s.TipoContenuto == TipiContenuto.Discussione && idDiscussioni.Contains(s.IdContenuto))
                .ExecuteDeleteAsync();

            await db.MiPiaceDiscussioni
                .Where(m => idDiscussioni.Contains(m.IdDiscussione))
                .ExecuteDeleteAsync();

            await db.Notifiche
                .Where(n => n.IdDiscussione != null && idDiscussioni.Contains(n.IdDiscussione.Value))
                .ExecuteDeleteAsync();

            await db.Discussioni
                .Where(d => idDiscussioni.Contains(d.IdDiscussione))
                .ExecuteDeleteAsync();
        }

        // Elimina le recensioni indicate con tutti i loro commenti e le segnalazioni collegate.
        public static async Task EliminaRecensioni(CinemaContext db, List<int> idRecensioni)
        {
            var idCommenti = await db.Commenti
                .Where(c => c.IdRecensione != null && idRecensioni.Contains(c.IdRecensione.Value))
                .Select(c => c.IdCommento)
                .ToListAsync();
            await EliminaCommenti(db, idCommenti);

            await db.Segnalazioni
                .Where(s => s.TipoContenuto == TipiContenuto.Recensione && idRecensioni.Contains(s.IdContenuto))
                .ExecuteDeleteAsync();

            await db.MiPiaceRecensioni
                .Where(m => idRecensioni.Contains(m.IdRecensione))
                .ExecuteDeleteAsync();

            await db.Notifiche
                .Where(n => n.IdRecensione != null && idRecensioni.Contains(n.IdRecensione.Value))
                .ExecuteDeleteAsync();

            await db.Recensioni
                .Where(r => idRecensioni.Contains(r.IdRecensione))
                .ExecuteDeleteAsync();
        }
    }
}
