using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // "Sondaggio della settimana": quello in corso è l'ultimo con DataInizio già passata, così una
    // scorta preparata in anticipo parte da sola. Un voto per utente, si può cambiare.
    public class SondaggiService
    {
        private readonly CinemaContext _db;

        public SondaggiService(CinemaContext db) { _db = db; }

        private IQueryable<Sondaggio> Iniziati() => _db.Sondaggi.Where(s => s.DataInizio <= DateTime.Now);

        public async Task<SondaggioDto?> Corrente(int? idUtente)
        {
            var id = await Iniziati().OrderByDescending(s => s.DataInizio).ThenByDescending(s => s.IdSondaggio)
                .Select(s => (int?)s.IdSondaggio).FirstOrDefaultAsync();
            return id == null ? null : await Dettaglio(id.Value, idUtente);
        }

        public async Task<SondaggioDto?> Dettaglio(int idSondaggio, int? idUtente)
        {
            var sondaggio = await _db.Sondaggi
                .Where(s => s.IdSondaggio == idSondaggio)
                .Select(s => new SondaggioDto
                {
                    IdSondaggio = s.IdSondaggio,
                    Domanda = s.Domanda,
                    DataInizio = s.DataInizio,
                    MiaOpzione = idUtente == null ? null : _db.VotiSondaggio
                        .Where(v => v.IdSondaggio == s.IdSondaggio && v.IdUtente == idUtente)
                        .Select(v => (int?)v.IdOpzione).FirstOrDefault(),
                    Opzioni = s.Opzioni!.OrderBy(o => o.Ordine).Select(o => new OpzioneSondaggioDto
                    {
                        IdOpzione = o.IdOpzione,
                        Testo = o.Testo,
                        IdFilm = o.Film != null && o.Film.Stato == StatiFilm.Approvato ? o.IdFilm : null,
                        ImmagineUrl = o.Film != null && o.Film.Stato == StatiFilm.Approvato ? o.Film.ImmagineUrl : null,
                        Voti = _db.VotiSondaggio.Count(v => v.IdOpzione == o.IdOpzione)
                    }).ToList()
                })
                .FirstOrDefaultAsync();
            if (sondaggio != null) sondaggio.TotaleVoti = sondaggio.Opzioni.Sum(o => o.Voti);
            return sondaggio;
        }

        public enum EsitoVoto { Ok, NonTrovato, NonIniziato, OpzioneNonValida }

        // Si vota solo un sondaggio già iniziato e con una sua risposta
        public async Task<EsitoVoto> Vota(int idSondaggio, int idUtente, int idOpzione)
        {
            var sondaggio = await _db.Sondaggi.Where(s => s.IdSondaggio == idSondaggio).Select(s => new { s.DataInizio }).FirstOrDefaultAsync();
            if (sondaggio == null) return EsitoVoto.NonTrovato;
            if (sondaggio.DataInizio > DateTime.Now) return EsitoVoto.NonIniziato;
            if (!await _db.OpzioniSondaggio.AnyAsync(o => o.IdOpzione == idOpzione && o.IdSondaggio == idSondaggio))
                return EsitoVoto.OpzioneNonValida;

            var aggiornati = await _db.VotiSondaggio
                .Where(v => v.IdSondaggio == idSondaggio && v.IdUtente == idUtente)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.IdOpzione, idOpzione).SetProperty(v => v.DataVoto, DateTime.Now));
            if (aggiornati > 0) return EsitoVoto.Ok;

            _db.VotiSondaggio.Add(new VotoSondaggio { IdSondaggio = idSondaggio, IdUtente = idUtente, IdOpzione = idOpzione, DataVoto = DateTime.Now });
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                // Due tocchi insieme: il primo ha già creato il voto, il secondo lo aggiorna
                _db.ChangeTracker.Clear();
                await _db.VotiSondaggio
                    .Where(v => v.IdSondaggio == idSondaggio && v.IdUtente == idUtente)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.IdOpzione, idOpzione));
            }
            return EsitoVoto.Ok;
        }

        public async Task<bool> TogliVoto(int idSondaggio, int idUtente)
        {
            if (!await _db.Sondaggi.AnyAsync(s => s.IdSondaggio == idSondaggio)) return false;
            await _db.VotiSondaggio.Where(v => v.IdSondaggio == idSondaggio && v.IdUtente == idUtente).ExecuteDeleteAsync();
            return true;
        }

        // ---- Pannello admin ----

        public async Task<List<SondaggioAdminDto>> Elenco()
        {
            var adesso = DateTime.Now;
            var idCorrente = await Iniziati().OrderByDescending(s => s.DataInizio).ThenByDescending(s => s.IdSondaggio)
                .Select(s => (int?)s.IdSondaggio).FirstOrDefaultAsync();
            var elenco = await _db.Sondaggi
                .OrderByDescending(s => s.DataInizio).ThenByDescending(s => s.IdSondaggio)
                .Select(s => new SondaggioAdminDto
                {
                    IdSondaggio = s.IdSondaggio,
                    Domanda = s.Domanda,
                    DataInizio = s.DataInizio,
                    Opzioni = s.Opzioni!.OrderBy(o => o.Ordine).Select(o => new OpzioneSondaggioDto
                    {
                        IdOpzione = o.IdOpzione,
                        Testo = o.Testo,
                        IdFilm = o.IdFilm,
                        ImmagineUrl = o.Film != null ? o.Film.ImmagineUrl : null,
                        Voti = _db.VotiSondaggio.Count(v => v.IdOpzione == o.IdOpzione)
                    }).ToList()
                })
                .ToListAsync();
            foreach (var s in elenco)
            {
                s.TotaleVoti = s.Opzioni.Sum(o => o.Voti);
                s.Stato = s.IdSondaggio == idCorrente ? "InCorso" : s.DataInizio > adesso ? "Programmato" : "Concluso";
            }
            return elenco;
        }

        // null se un titolo indicato non è nel catalogo
        public async Task<int?> Crea(CreaSondaggioDto dto)
        {
            var idFilm = dto.Opzioni.Where(o => o.IdFilm != null).Select(o => o.IdFilm!.Value).Distinct().ToList();
            if (idFilm.Count > 0 && await _db.Film.CountAsync(f => idFilm.Contains(f.IdFilm) && f.Stato == StatiFilm.Approvato) != idFilm.Count)
                return null;

            var sondaggio = new Sondaggio
            {
                Domanda = dto.Domanda.Trim(),
                DataInizio = dto.DataInizio ?? DateTime.Now,
                DataCreazione = DateTime.Now,
                Opzioni = dto.Opzioni.Select((o, i) => new OpzioneSondaggio { Testo = o.Testo.Trim(), IdFilm = o.IdFilm, Ordine = i + 1 }).ToList()
            };
            _db.Sondaggi.Add(sondaggio);
            await _db.SaveChangesAsync();
            return sondaggio.IdSondaggio;
        }

        // Le risposte e i loro voti se ne vanno con il sondaggio (ON DELETE CASCADE)
        public async Task<bool> Elimina(int idSondaggio)
        {
            await _db.VotiSondaggio.Where(v => v.IdSondaggio == idSondaggio).ExecuteDeleteAsync();
            return await _db.Sondaggi.Where(s => s.IdSondaggio == idSondaggio).ExecuteDeleteAsync() > 0;
        }
    }
}
