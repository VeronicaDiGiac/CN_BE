using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    public class UtentiService
    {
        private readonly CinemaContext _db;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UtentiService(CinemaContext db, IWebHostEnvironment webHostEnvironment, IConfiguration config)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
            _config = config;
        }

        public async Task<bool> UpdateProfilo(int id, UpdateProfiloDto dto)
        {
            var utente = await _db.Utenti.FindAsync(id);
            if (utente == null) return false;

            // Aggiorna SOLO i campi effettivamente forniti (!= null),
            // così un aggiornamento parziale non azzera i campi non inviati.
            if (dto.UserName != null)
            {
                utente.UserName = dto.UserName;
            }
            if (dto.Bio != null)
            {
                utente.Bio = dto.Bio;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<UtenteProfiloDto?> GetProfilo(int? idUtente)
        {
            var profilo = await _db.Utenti
                .Where(u => u.IdUtente == idUtente)
                .Select(u => new UtenteProfiloDto
                {
                    IdUtente = u.IdUtente,
                    UserName = u.UserName,
                    Bio = u.Bio,
                    AvatarUrl = u.AvatarUrl,
                    DataDiRegistrazione = u.DataDiRegistrazione
                })
                .FirstOrDefaultAsync();
            return profilo;
        }

        // Eliminazione a cascata: recensioni, commenti, preferiti, follow e segnalazioni
        // dell'utente. I film che ha proposto restano nel catalogo, senza proprietario.
        public async Task<bool> DeleteProfilo(int id)
        {
            var profilo = await _db.Utenti.FindAsync(id);
            if (profilo == null) return false;

            using var transazione = await _db.Database.BeginTransactionAsync();

            // Prima le recensioni (si portano via anche i commenti degli altri sotto di esse),
            // poi i commenti che l'utente ha scritto sotto recensioni altrui.
            var idRecensioni = await _db.Recensioni
                .Where(r => r.IdUtente == id)
                .Select(r => r.IdRecensione)
                .ToListAsync();
            await EliminazioneHelper.EliminaRecensioni(_db, idRecensioni);

            // I fili dei commenti degli episodi aperti da lui non sono "suoi": passano a chi altro ha commentato
            // (il primo in ordine di tempo); se ha commentato solo lui si eliminano con le sue discussioni
            var fili = await _db.Discussioni
                .Where(d => d.IdUtente == id && d.FiloEpisodio)
                .Select(d => new
                {
                    d.IdDiscussione,
                    Altro = _db.Commenti.Where(c => c.IdDiscussione == d.IdDiscussione && c.IdUtente != id)
                        .OrderBy(c => c.DataCommento).Select(c => (int?)c.IdUtente).FirstOrDefault()
                })
                .ToListAsync();
            foreach (var f in fili.Where(f => f.Altro != null))
                await _db.Discussioni.Where(d => d.IdDiscussione == f.IdDiscussione)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.IdUtente, f.Altro!.Value));

            // Le sue discussioni (con i messaggi degli altri dentro)
            var idDiscussioni = await _db.Discussioni
                .Where(d => d.IdUtente == id)
                .Select(d => d.IdDiscussione)
                .ToListAsync();
            await EliminazioneHelper.EliminaDiscussioni(_db, idDiscussioni);

            var idCommenti = await _db.Commenti
                .Where(c => c.IdUtente == id)
                .Select(c => c.IdCommento)
                .ToListAsync();
            await EliminazioneHelper.EliminaCommenti(_db, idCommenti);

            await _db.Preferiti.Where(p => p.IdUtente == id).ExecuteDeleteAsync();
            await _db.Follow.Where(f => f.FollowerId == id || f.FollowingId == id).ExecuteDeleteAsync();
            await _db.Segnalazioni.Where(s => s.IdUtente == id).ExecuteDeleteAsync();
            await _db.PasswordReset.Where(p => p.IdUtente == id).ExecuteDeleteAsync();
            await _db.Visioni.Where(v => v.IdUtente == id).ExecuteDeleteAsync();
            await _db.EpisodiVisti.Where(v => v.IdUtente == id).ExecuteDeleteAsync();
            await _db.DaVedere.Where(d => d.IdUtente == id).ExecuteDeleteAsync();
            await _db.Salvati.Where(s => s.IdUtente == id).ExecuteDeleteAsync();
            await _db.MiPiaceRecensioni.Where(m => m.IdUtente == id).ExecuteDeleteAsync();
            await _db.MiPiaceCommenti.Where(m => m.IdUtente == id).ExecuteDeleteAsync();
            await _db.MiPiaceDiscussioni.Where(m => m.IdUtente == id).ExecuteDeleteAsync();
            await _db.Notifiche.Where(n => n.IdDestinatario == id || n.IdMittente == id).ExecuteDeleteAsync();
            await _db.ListeFilm.Where(lf => lf.Lista!.IdUtente == id).ExecuteDeleteAsync();
            await _db.Liste.Where(l => l.IdUtente == id).ExecuteDeleteAsync();
            await _db.Segnalazioni
                .Where(s => s.IdAdminGestore == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IdAdminGestore, (int?)null));
            await _db.Film
                .Where(f => f.IdUtente == id)
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.IdUtente, (int?)null));

            _db.Utenti.Remove(profilo);
            await _db.SaveChangesAsync();
            await transazione.CommitAsync();
            return true;
        }

        public async Task<string?> SaveAvatar(int id, IFormFile file)
        {
            var utente = await _db.Utenti.FindAsync(id);
            if (utente == null) return null;

            var estensione = Path.GetExtension(file.FileName);
            // Genera un nome univoco tipo: a3f29c1e-8b4d-4f2a-9c3e-1d5f7a8b9c0d.jpg
            var nomeFileUnivoco = $"{Guid.NewGuid()}{estensione}";

            // Percorso fisico della cartella, es: C:\...\wwwroot\uploads\avatar
            var cartellaUpload = Path.Combine(PercorsiUpload.Radice(_config, _webHostEnvironment), "avatar");
            // Alla prima immagine (o in una cartella nuova, es. in produzione) la cartella non c'è ancora
            Directory.CreateDirectory(cartellaUpload);
            var percorsoCompleto = Path.Combine(cartellaUpload, nomeFileUnivoco);

            // Salva il file sul disco fisico
            using (var stream = new FileStream(percorsoCompleto, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // URL relativo pubblico (corretto, senza refusi)
            var urlRelativo = $"/uploads/avatar/{nomeFileUnivoco}";
            utente.AvatarUrl = urlRelativo;
            await _db.SaveChangesAsync();

            return urlRelativo;
        }
    }
}