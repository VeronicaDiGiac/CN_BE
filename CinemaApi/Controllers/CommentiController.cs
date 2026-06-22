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
    public class CommentiController : ControllerBase
    {
        private readonly CommentiService _commentiService;
        public CommentiController(CommentiService commentiService) { _commentiService = commentiService; }

        [HttpGet]
        [ProducesResponseType(typeof(List<CommentoDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Get(int? idRecensione, int? idUtente)
        {
            try
            {
                var commenti = await _commentiService.GetCommenti(idRecensione, idUtente);
                return Ok(commenti);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateCommentoDto dto)
        {
            try
            {
                var commento = await _commentiService.CreateCommento(dto);
                return CreatedAtAction(nameof(Create), commento);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Update(int id, [FromBody] CreateCommentoDto dto)
        {
            try
            {
                var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var ruolo = User.FindFirst(ClaimTypes.Role).Value;
                var commenti = await _commentiService.GetCommentoById(id);
                if (commenti == null) return NotFound();
                if (!AutorizzazioneHelper.PuoModificare(commenti.IdUtente, idUtenteAutenticato, ruolo)) return Forbid();
                var aggiornato = await _commentiService.UpdateCommento(id, dto);
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
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var ruolo = User.FindFirst(ClaimTypes.Role).Value;
                var commenti = await _commentiService.GetCommentoById(id);
                if (commenti == null) return NotFound();
                if (!AutorizzazioneHelper.PuoModificare(commenti.IdUtente, idUtenteAutenticato, ruolo)) return Forbid();
                var eliminato = await _commentiService.DeleteCommento(id);
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