using CinemaApi.DTOs.ResponseDto;
using CinemaApi.DTOs.RequestDto;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    public class FilmService
    {
        private readonly CinemaContext _db;
        public FilmService(CinemaContext db)
        {
            _db = db;
        }

        // idUtenteAutenticato è nullable: null se chi chiama non è loggato,
        // in quel caso GiaRecensito resta sempre false.
        public async Task<FilmDettaglioDto?> GetFilmDettaglio(int id, int? idUtenteAutenticato)
        {
            var film = await _db.Film
                .Where(f => f.IdFilm == id)
                .Select(f => new FilmDettaglioDto
                {
                    IdFilm = f.IdFilm,
                    Titolo = f.Titolo,
                    Regista = f.Regista,
                    AnnoUscita = f.AnnoUscita,
                    Genere = f.Genere,
                    Attori = f.Attori,
                    ImmagineUrl = f.ImmagineUrl,
                    Descrizione = f.Descrizione,
                    VotoMedio = f.Recensioni.Any() ? f.Recensioni.Average(r => r.Voto) : null,
                    NumeroRecensioni = f.Recensioni.Count(),
                    GiaRecensito = idUtenteAutenticato != null &&
                                    f.Recensioni.Any(r => r.IdUtente == idUtenteAutenticato)
                })
                .FirstOrDefaultAsync();

            return film;
        }

        public async Task<Film?> GetFilmById(int id)
        {
            return await _db.Film.FindAsync(id);
        }



        // PAGINAZIONE — esempio concreto per ricordare la formula Skip/Take:
        //
        // Supponiamo 50 film totali, pageSize = 20 (20 film per pagina).
        //
        // pageNumber = 1  →  Skip = (1-1) * 20 = 0   → salto 0 film, prendo i film  1-20
        // pageNumber = 2  →  Skip = (2-1) * 20 = 20  → salto i primi 20, prendo i film 21-40
        // pageNumber = 3  →  Skip = (3-1) * 20 = 40  → salto i primi 40, prendo i film 41-50
        //
        // Regola generale: "quante pagine intere ho già mostrato prima di questa"
        // (pageNumber - 1) moltiplicato per "quanti elementi ci sono in ogni pagina"
        // (pageSize) = quanti elementi devo SALTARE prima di iniziare a prendere
        // quelli della pagina richiesta.

        public async Task<PagedResultDto<Film>> GetFilm(string? titolo, string? regista, int? annoUscita, string? genere, string? attori, int pageNumber, int pageSize)
        {
              //Da Ricordare : la query costruita non è ancora eseguita, la uso due volte, una volta per contare il totale una volta per prendere la pagina specifica, se eseguissi ToListAsync non si potrebbe contare niente.
            var query = _db.Film
                .Where(f => titolo == null || f.Titolo.Contains(titolo))
                .Where(f => regista == null || f.Regista.Contains(regista))
                .Where(f => genere == null || f.Genere.Contains(genere))
                .Where(f => annoUscita == null || f.AnnoUscita == annoUscita)
                .Where(f => attori == null || f.Attori.Contains(attori));

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<Film>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            };
        }

        public async Task<Film> CreateFilm(CreateFilmDto dto, int idUtente)
        {
            var film = new Film
            {
                Titolo = dto.Titolo,
                Regista = dto.Regista,
                AnnoUscita = dto.AnnoUscita,
                Genere = dto.Genere,
                Attori = dto.Attori,
                ImmagineUrl = dto.ImmagineUrl,
                Descrizione = dto.Descrizione,
                IdUtente = idUtente
            };
            _db.Film.Add(film);
            await _db.SaveChangesAsync();
            return film;
        }

        public async Task<bool> UpdateFilm(int id, CreateFilmDto dto)
        {
            var film = await _db.Film.FindAsync(id);
            if (film == null) return false;
            film.Titolo = dto.Titolo;
            film.Regista = dto.Regista;
            film.AnnoUscita = dto.AnnoUscita;
            film.Genere = dto.Genere;
            film.Attori = dto.Attori;
            film.ImmagineUrl = dto.ImmagineUrl;
            film.Descrizione = dto.Descrizione;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteFilm(int id)
        {
            var film = await _db.Film.FindAsync(id);
            if (film == null) return false;
            _db.Film.Remove(film);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}