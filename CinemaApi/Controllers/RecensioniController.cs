using CinemaApi.DTOs;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecensioniController : ControllerBase
    {
        private readonly RecensioniService _recensioniService;
        public RecensioniController(RecensioniService recensioniService) { _recensioniService = recensioniService; }

        [HttpGet]
        [ProducesResponseType(typeof(List<RecensioneDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Get(int? idFilm, int? idUtente)
        {
            try
            {
                var recensioni = await _recensioniService.GetRecensioni(idFilm, idUtente);
                return Ok(recensioni);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateRecensioneDto dto)
        {
            try
            {
                var recensione = await _recensioniService.CreateRecensione(dto);
                if (recensione == null) return Conflict("Hai già lasciato una recensione per questo film");
                return CreatedAtAction(nameof(Create), recensione);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Update(int id, [FromBody] CreateRecensioneDto dto)
        {
            try
            {
                var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var recensione = await _recensioniService.GetRecensioneById(id);
                if (recensione == null) return NotFound();
                if (recensione.IdUtente != idUtenteAutenticato) return Forbid();
                await _recensioniService.UpdateRecensione(id, dto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var recensione = await _recensioniService.GetRecensioneById(id);
                if (recensione == null) return NotFound();
                if (recensione.IdUtente != idUtenteAutenticato) return Forbid();
                var eliminata = await _recensioniService.DeleteRecensione(id);
                if (!eliminata) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}