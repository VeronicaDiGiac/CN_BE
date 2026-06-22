using CinemaApi.DTOs;
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

        public async Task <FilmDettaglioDto?> GetFilmDettaglio (int id)
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
                    NumeroRecensioni = f.Recensioni.Count()
                })
                .FirstOrDefaultAsync();

            return film;
        } 
        public async Task<List<Film>> GetFilm(string? titolo, string? regista, int? annoUscita, string? genere, string? attori)
        {
            return await _db.Film
                .Where(f => titolo == null || f.Titolo.Contains(titolo))
                .Where(f => regista == null || f.Regista.Contains(regista))
                .Where(f => genere == null || f.Genere.Contains(genere))
                .Where(f => annoUscita == null || f.AnnoUscita == annoUscita)
                .Where (f => attori == null || f.Attori.Contains(attori))
                .ToListAsync();
        }

        public async Task<Film?> GetFilmById(int id)
        {
            return await _db.Film.FindAsync(id);
        }

//Passo idutente come parametro separato e non nel dto accessibile da browser
        public async Task<Film> CreateFilm(CreateFilmDto dto, int IdUtente)
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
                IdUtente = IdUtente
            };
            _db.Film.Add(film);
            await _db.SaveChangesAsync();
            return film;
        }

        // Ritorna true se aggiornato, false se il film non esiste
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

        // Ritorna true se eliminato, false se il film non esiste
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