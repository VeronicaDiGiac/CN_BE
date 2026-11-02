using CinemaApi.DTOs.RequestDto;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            var preferiti = await _preferitiService.GetPreferiti(idUtente);
            return Ok(preferiti);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreatePreferitoDto dto)
        {
            var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (!await _preferitiService.FilmEsiste(dto.IdFilm)) return NotFound("Film non trovato");
            var preferito = await _preferitiService.CreatePreferito(dto, idUtenteAutenticato);
            if (preferito == null) return Conflict("Film già presente nei preferiti");
            return CreatedAtAction(nameof(Create), preferito);
        }

        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var preferito = await _preferitiService.GetPreferitoById(id);
            if (preferito == null) return NotFound();

            // Controllo proprietario: puoi eliminare solo i TUOI preferiti.
            // Niente eccezione Admin: i preferiti sono personali, un Admin non
            // ha motivo di toccare i preferiti altrui.
            if (preferito.IdUtente != idUtenteAutenticato) return Forbid();

            var eliminato = await _preferitiService.DeletePreferito(id);
            if (!eliminato) return NotFound();
            return NoContent();
        }
    }
}