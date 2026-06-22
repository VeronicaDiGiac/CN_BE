using CinemaApi.DTOs;
using CinemaApi.Models;
using CinemaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilmController : ControllerBase
    {
        private readonly FilmService _filmService;
        public FilmController(FilmService filmService) { _filmService = filmService; }

        [HttpGet("{id}/dettaglio")]
        [ProducesResponseType(typeof(FilmDettaglioDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDettaglio(int id)
        {
            try
            {
                var film = await _filmService.GetFilmDettaglio(id);
                if (film == null) return NotFound();
                return Ok(film);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

            [HttpGet]
            //Per ogni possibile risposta del metodo produce un responsetype
            [ProducesResponseType(typeof(List<Film>), 200)]
            [ProducesResponseType(500)]
        public async Task<IActionResult> Get(string? titolo, string? regista, int? annoUscita, string? genere, string? attori)
            {
                try
                {
                    var film = await _filmService.GetFilm(titolo, regista, annoUscita, genere, attori);
                    return Ok(film);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            [HttpGet("{id}")]
            [ProducesResponseType(typeof(Film), 200)]
            [ProducesResponseType(404)]
            [ProducesResponseType(500)]
        public async Task<IActionResult> GetById(int id)
            {
                try
                {
                    var film = await _filmService.GetFilmById(id);
                    if (film == null) return NotFound();
                    return Ok(film);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            [HttpPost]
            [ProducesResponseType(typeof(Film), 201)]
            [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateFilmDto dto, int IdUtente)
            {
                try
                {
                    var film = await _filmService.CreateFilm(dto, IdUtente);
                    return CreatedAtAction(nameof(Create), film);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            [HttpPut("{id}")]
            [ProducesResponseType(204)]
            [ProducesResponseType(404)]
            [ProducesResponseType(500)]
        public async Task<IActionResult> Update(int id, [FromBody] CreateFilmDto dto)
            {
                try
                {
                    var aggiornato = await _filmService.UpdateFilm(id, dto);
                    if (!aggiornato) return NotFound();
                    return NoContent();
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            [HttpDelete("{id}")]
            [ProducesResponseType(204)]
            [ProducesResponseType(404)]
            [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
            {
                try
                {
                    var eliminato = await _filmService.DeleteFilm(id);
                    if (!eliminato) return NotFound();
                    return NoContent();
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }
        }
    }
