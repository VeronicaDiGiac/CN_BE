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
            var commenti = await _commentiService.GetCommenti(idRecensione, idUtente);
            return Ok(commenti);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateCommentoDto dto)
        {
            var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (!await _commentiService.RecensioneEsiste(dto.IdRecensione)) return NotFound("Recensione non trovata");
            if (dto.IdCommentoPadre != null && !await _commentiService.CommentoPadreValido(dto.IdCommentoPadre.Value, dto.IdRecensione))
                return BadRequest("Il commento a cui rispondi non esiste o appartiene a un'altra recensione");
            var commento = await _commentiService.CreateCommento(dto, idUtenteAutenticato);
            return CreatedAtAction(nameof(Create), commento);
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Update(int id, [FromBody] CreateCommentoDto dto)
        {
            var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var ruolo = User.FindFirst(ClaimTypes.Role).Value;
            var commenti = await _commentiService.GetCommentoById(id);
            if (commenti == null) return NotFound();
            if (!AutorizzazioneHelper.PuoModificare(commenti.IdUtente, idUtenteAutenticato, ruolo)) return Forbid();
            var aggiornato = await _commentiService.UpdateCommento(id, dto);
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
            var commenti = await _commentiService.GetCommentoById(id);
            if (commenti == null) return NotFound();
            if (!AutorizzazioneHelper.PuoModificare(commenti.IdUtente, idUtenteAutenticato, ruolo)) return Forbid();
            var eliminato = await _commentiService.DeleteCommento(id);
            if (!eliminato) return NotFound();
            return NoContent();
        }
    }
}