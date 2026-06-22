using CinemaApi.DTOs;
using CinemaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PreferitiController : ControllerBase
    {
        private readonly PreferitiService _preferitiService;
        public PreferitiController(PreferitiService preferitiService) { _preferitiService = preferitiService; }

        [HttpGet]
        [ProducesResponseType(typeof(List<object>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Get(int? idUtente)
        {
            try
            {
                var preferiti = await _preferitiService.GetPreferiti(idUtente);
                return Ok(preferiti);
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
        public async Task<IActionResult> Create([FromBody] CreatePreferitoDto dto)
        {
            try
            {
                var preferito = await _preferitiService.CreatePreferito(dto);
                if (preferito == null) return Conflict("Film già presente nei preferiti");
                return CreatedAtAction(nameof(Create), preferito);
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
                var eliminato = await _preferitiService.DeletePreferito(id);
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