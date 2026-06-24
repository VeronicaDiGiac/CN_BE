using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtentiController : ControllerBase
    {
        private readonly UtentiService _utentiService;
        public UtentiController(UtentiService utentiService) { _utentiService = utentiService; }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UtenteProfiloDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProfilo(int id)
        {
            try
            {
                var profilo = await _utentiService.GetProfilo(id);
                if (profilo == null) return NotFound();
                return Ok(profilo);
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
        public async Task<IActionResult> UpdateProfilo(int id, [FromBody] UpdateProfiloDto dto)
        {
            try
            {
                var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                // Niente AutorizzazioneHelper qui: per il profilo personale
                // SOLO il proprietario può modificare, nessuna eccezione Admin.
                if (id != idUtenteAutenticato) return Forbid();

                var aggiornato = await _utentiService.UpdateProfilo(id, dto);
                if (!aggiornato) return NotFound();
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
        public async Task<IActionResult> DeleteProfilo(int id)
        {
            try
            {
                var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                // Stessa regola di UpdateProfilo: solo il proprietario,
                // nessuna eccezione Admin (per ora, vedi nota in TODO).
                if (id != idUtenteAutenticato) return Forbid();

                var eliminato = await _utentiService.DeleteProfilo(id);
                if (!eliminato) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{id}/avatar")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UploadAvatar(int id, IFormFile file)
        {
            try
            {
                if (!FileValidationHelper.EImmagineValida(file, out var errore))
                    return BadRequest(errore);
                if (!await FileValidationHelper.HaFirmaBinariaValida(file))
                    return BadRequest("Il file non sembra essere un'immagine valida");
                var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var ruolo = User.FindFirst(ClaimTypes.Role).Value;
                var utente = await _utentiService.GetProfilo(id);
                if (utente == null) return NotFound();
                //Controllo: Per ora l'admin non può fare upload dell'avatar al posto dell'utente 
                if (id != idUtenteAutenticato) return Forbid();

                var updateUrlImage = await _utentiService.SaveAvatar(id, file);
                //Capire se lasciarlo il doppio controllo 
                if (updateUrlImage == null) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}