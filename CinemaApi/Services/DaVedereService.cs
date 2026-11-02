using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Lista "Da vedere" (watchlist) di ogni utente
    public class DaVedereService
    {
        private readonly CinemaContext _db;

        public DaVedereService(CinemaContext db) { _db = db; }

        public Task<bool> FilmEsiste(int idFilm) => _db.FilmApprovatoEsiste(idFilm);

        // Dalla più recente: è l'ordine in cui di solito si consulta una watchlist
        public Task<List<DaVedereDto>> GetLista(int idUtente) =>
            _db.DaVedere
                .Where(d => d.IdUtente == idUtente)
                .OrderByDescending(d => d.DataAggiunta)
                .Select(d => new DaVedereDto
                {
                    IdFilm = d.IdFilm,
                    TitoloFilm = d.Film!.Titolo,
                    Regista = d.Film.Regista,
                    AnnoUscita = d.Film.AnnoUscita,
                    ImmagineUrl = d.Film.ImmagineUrl,
                    DataAggiunta = d.DataAggiunta
                })
                .ToListAsync();

        // Idempotente: se il film è già nella lista non succede niente
        public async Task Aggiungi(int idUtente, int idFilm)
        {
            if (await _db.DaVedere.AnyAsync(d => d.IdUtente == idUtente && d.IdFilm == idFilm)) return;

            _db.DaVedere.Add(new DaVedere { IdUtente = idUtente, IdFilm = idFilm, DataAggiunta = DateTime.UtcNow });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Due richieste contemporanee: il vincolo UNIQUE ha già fatto il suo lavoro
            }
        }

        // false se il film non era nella lista
        public async Task<bool> Rimuovi(int idUtente, int idFilm) =>
            await _db.DaVedere.Where(d => d.IdUtente == idUtente && d.IdFilm == idFilm).ExecuteDeleteAsync() > 0;
    }
}
