using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilmController : ControllerBase
    {
        private readonly FilmService _filmService;
        public FilmController(FilmService filmService) { _filmService = filmService; }

        [HttpGet("{id}/dettaglio")]
        [ProducesResponseType(typeof(CinemaApi.DTOs.ResponseDto.FilmDettaglioDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDettaglio(int id)
        {
            // Autenticazione OPZIONALE: niente [Authorize] qui, l'endpoint
            // resta pubblico. Se però arriva un token valido, lo leggiamo
            // comunque per popolare GiaRecensito; altrimenti resta null
            // e GiaRecensito sarà sempre false.
            int? idUtenteAutenticato = null;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                idUtenteAutenticato = int.Parse(claim.Value);
            }

            var film = await _filmService.GetFilmDettaglio(id, idUtenteAutenticato);
            if (film == null) return NotFound();
            return Ok(film);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<Film>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Get(string? titolo, string? regista, int? annoUscita, string? genere, string? attori, int pageNumber = 1, int pageSize = 20)
        {
            var risultato = await _filmService.GetFilm(titolo, regista, annoUscita, genere, attori, pageNumber, pageSize);
            return Ok(risultato);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CinemaApi.Models.Film), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetById(int id)
        {
            var film = await _filmService.GetFilmById(id);
            if (film == null) return NotFound();
            if (film.Stato != StatiFilm.Approvato) return NotFound();
            return Ok(film);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(CinemaApi.Models.Film), 201)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateFilmDto dto)
        {
            var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var ruolo = User.FindFirst(ClaimTypes.Role).Value;
            var film = await _filmService.CreateFilm(dto, idUtenteAutenticato, ruolo);
            if (film == null) return Conflict("Esiste già un film con questo titolo");
            return CreatedAtAction(nameof(Create), film);
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Update(int id, [FromBody] CreateFilmDto dto)
        {
            var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var ruolo = User.FindFirst(ClaimTypes.Role).Value;
            var film = await _filmService.GetFilmById(id);
            if (film == null) return NotFound();
            if (!AutorizzazioneHelper.PuoModificare(film.IdUtente, idUtenteAutenticato, ruolo)) return Forbid();
            var aggiornato = await _filmService.UpdateFilm(id, dto);
            if (!aggiornato) return NotFound();
            return NoContent();
        }



        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var ruolo = User.FindFirst(ClaimTypes.Role).Value;
            var film = await _filmService.GetFilmById(id);
            if (film == null) return NotFound();
            if (!AutorizzazioneHelper.PuoModificare(film.IdUtente, idUtenteAutenticato, ruolo)) return Forbid();
            var eliminato = await _filmService.DeleteFilm(id);
            if (!eliminato) return NotFound();
            return NoContent();
        }
    

        [HttpPost("{id}/immagine")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UploadImmagine(int id, IFormFile file)
        {
            if (!FileValidationHelper.EImmagineValida(file, out var errore))
                return BadRequest(errore);
            if (!await FileValidationHelper.HaFirmaBinariaValida(file))
                return BadRequest("Il file non sembra essere un'immagine valida");
            var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var ruolo = User.FindFirst(ClaimTypes.Role).Value;
            var film = await _filmService.GetFilmById(id);
            if (film == null) return NotFound();
            if (!AutorizzazioneHelper.PuoModificare(film.IdUtente, idUtenteAutenticato, ruolo)) return Forbid();
           
            var updateUrlImage = await _filmService.SaveImage(id, file);
            //Capire se lasciarlo il doppio controllo 
            if (updateUrlImage == null) return NotFound();
            return NoContent();
        }

        [HttpPut("{id}/stato")]
        [Authorize(Roles = Ruoli.Admin)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateStatoFilm(int id, [FromBody] UpdateStatoFilmDto dto)
        {
            var aggiornato = await _filmService.UpdateStatoFilm(id, dto.Stato);
            if (!aggiornato) return NotFound();
            return NoContent();
        }

        [HttpGet("inattesa")]
        [Authorize(Roles = Ruoli.Admin)]
        [ProducesResponseType(typeof(List<Film>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetInAttesa()
        {
            var film = await _filmService.GetFilmInAttesa();
            return Ok(film);
        }
    }
}