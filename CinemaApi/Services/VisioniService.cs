using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Diario delle visioni ("Visto") di ogni utente
    public class VisioniService
    {
        private readonly CinemaContext _db;

        public VisioniService(CinemaContext db) { _db = db; }

        public Task<bool> FilmEsiste(int idFilm) => _db.FilmApprovatoEsiste(idFilm);

        public Task<Visione?> GetVisioneById(int id) => _db.Visioni.FindAsync(id).AsTask();

        // Diario di un utente, dalla visione più recente. Pubblico come il resto del profilo.
        public async Task<PagedResultDto<VisioneDto>> GetDiario(int idUtente, int pageNumber, int pageSize)
        {
            var query = _db.Visioni.Where(v => v.IdUtente == idUtente);
            var totale = await query.CountAsync();

            var voci = await query
                .OrderByDescending(v => v.DataVisione)
                .ThenByDescending(v => v.IdVisione)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VisioneDto
                {
                    IdVisione = v.IdVisione,
                    IdUtente = v.IdUtente,
                    IdFilm = v.IdFilm,
                    TitoloFilm = v.Film!.Titolo,
                    AnnoUscita = v.Film.AnnoUscita,
                    ImmagineUrl = v.Film.ImmagineUrl,
                    DataVisione = v.DataVisione,
                    Voto = _db.Recensioni
                        .Where(r => r.IdUtente == v.IdUtente && r.IdFilm == v.IdFilm)
                        .Select(r => r.Voto)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new PagedResultDto<VisioneDto>
            {
                TotalCount = totale,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = voci
            };
        }

        // Segnare un film come visto lo toglie anche dalla lista "Da vedere"
        public async Task<VisioneDto> CreateVisione(CreateVisioneDto dto, int idUtente)
        {
            var visione = new Visione
            {
                IdUtente = idUtente,
                IdFilm = dto.IdFilm,
                DataVisione = dto.DataVisione ?? DateOnly.FromDateTime(DateTime.Now),
                DataCreazione = DateTime.UtcNow
            };

            using var transazione = await _db.Database.BeginTransactionAsync();
            _db.Visioni.Add(visione);
            await _db.SaveChangesAsync();
            await _db.DaVedere.Where(d => d.IdUtente == idUtente && d.IdFilm == dto.IdFilm).ExecuteDeleteAsync();
            await transazione.CommitAsync();

            var film = await _db.Film.FindAsync(dto.IdFilm);
            return new VisioneDto
            {
                IdVisione = visione.IdVisione,
                IdUtente = idUtente,
                IdFilm = visione.IdFilm,
                TitoloFilm = film?.Titolo,
                AnnoUscita = film?.AnnoUscita,
                ImmagineUrl = film?.ImmagineUrl,
                DataVisione = visione.DataVisione
            };
        }

        public async Task DeleteVisione(Visione visione)
        {
            _db.Visioni.Remove(visione);
            await _db.SaveChangesAsync();
        }
    }
}
