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

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(VisioneDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Create([FromBody] CreateVisioneDto dto)
        {
            if (dto.DataVisione > DateOnly.FromDateTime(DateTime.Now))
                return BadRequest("La data di visione non può essere nel futuro");
            if (!await _visioniService.FilmEsiste(dto.IdFilm)) return NotFound("Film non trovato");
            if (await _visioniService.ESerie(dto.IdFilm)) return BadRequest("Per le serie TV si segnano gli episodi visti");

            var visione = await _visioniService.CreateVisione(dto, User.IdUtente());
            return CreatedAtAction(nameof(Create), visione);
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
