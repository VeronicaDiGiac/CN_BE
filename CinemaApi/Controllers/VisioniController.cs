using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisioniController : ControllerBase
    {
        private const int PageSizeMassimo = 100;

        private readonly VisioniService _visioniService;
        public VisioniController(VisioniService visioniService) { _visioniService = visioniService; }

        // Diario pubblico di un utente
        [HttpGet("utente/{idUtente}")]
        [ProducesResponseType(typeof(PagedResultDto<VisioneDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetDiario(int idUtente, int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1 || pageSize < 1 || pageSize > PageSizeMassimo)
                return BadRequest("Paginazione non valida");
            return Ok(await _visioniService.GetDiario(idUtente, pageNumber, pageSize));
        }

        // Segna il film come visto: una volta sola per utente (409 se lo è già)
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(VisioneDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateVisioneDto dto)
        {
            // Data di visione non più scelta dall'utente (vedi CreateVisioneDto)
            // if (dto.DataVisione > DateOnly.FromDateTime(DateTime.Now))
            //     return BadRequest("La data di visione non può essere nel futuro");
            if (!await _visioniService.FilmEsiste(dto.IdFilm)) return NotFound("Film non trovato");
            if (await _visioniService.ESerie(dto.IdFilm)) return BadRequest("Per le serie TV si segnano gli episodi visti");

            var visione = await _visioniService.CreateVisione(dto, User.IdUtente());
            if (visione == null) return Conflict("Film già segnato come visto");
            return CreatedAtAction(nameof(Create), visione);
        }

        // "Non visto": toglie il film dai visti dell'utente
        [HttpDelete("film/{idFilm}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TogliVisto(int idFilm)
        {
            if (!await _visioniService.TogliVisto(User.IdUtente(), idFilm)) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id)
        {
            var visione = await _visioniService.GetVisioneById(id);
            if (visione == null) return NotFound();
            // Il diario è personale: si eliminano solo le proprie visioni
            if (visione.IdUtente != User.IdUtente()) return Forbid();

            await _visioniService.DeleteVisione(visione);
            return NoContent();
        }
    }
}
