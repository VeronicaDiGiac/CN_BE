using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CinemaApi.Controllers
{
    // Commenti delle recensioni e messaggi delle discussioni
    [ApiController]
    [Route("api/[controller]")]
    public class CommentiController : ControllerBase
    {
        private readonly CommentiService _commentiService;
        public CommentiController(CommentiService commentiService) { _commentiService = commentiService; }

        [HttpGet]
        [ProducesResponseType(typeof(List<CommentoDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Get(int? idRecensione, int? idUtente, int? idDiscussione)
        {
            var commenti = await _commentiService.GetCommenti(idRecensione, idUtente, idDiscussione, User.IdUtenteOpzionale());
            return Ok(commenti);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(CommentoDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateCommentoDto dto)
        {
            var idUtenteAutenticato = User.IdUtente();
            // Sotto una recensione oppure in una discussione: esattamente uno dei due
            if ((dto.IdRecensione == null) == (dto.IdDiscussione == null))
                return BadRequest("Indica la recensione oppure la discussione");
            if (dto.IdRecensione != null && !await _commentiService.RecensioneEsiste(dto.IdRecensione.Value)) return NotFound("Recensione non trovata");
            if (dto.IdDiscussione != null && !await _commentiService.DiscussioneEsiste(dto.IdDiscussione.Value)) return NotFound("Discussione non trovata");
            if (dto.IdCommentoPadre != null && !await _commentiService.CommentoPadreValido(dto.IdCommentoPadre.Value, dto.IdRecensione, dto.IdDiscussione))
                return BadRequest("Il commento a cui rispondi non esiste o sta altrove");
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
            var idUtenteAutenticato = User.IdUtente();
            var ruolo = User.Ruolo();
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
            var idUtenteAutenticato = User.IdUtente();
            var ruolo = User.Ruolo();
            var commenti = await _commentiService.GetCommentoById(id);
            if (commenti == null) return NotFound();
            if (!AutorizzazioneHelper.PuoModificare(commenti.IdUtente, idUtenteAutenticato, ruolo)) return Forbid();
            var eliminato = await _commentiService.DeleteCommento(id);
            if (!eliminato) return NotFound();
            return NoContent();
        }

        // PUT perché è idempotente: ripeterlo non aggiunge un secondo "mi piace"
        [HttpPut("{id}/mipiace")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MettiMiPiace(int id)
        {
            var commento = await _commentiService.GetCommentoById(id);
            if (commento == null) return NotFound();
            if (commento.IdUtente == User.IdUtente()) return BadRequest("Non puoi mettere \"mi piace\" al tuo commento");
            await _commentiService.MettiMiPiace(commento, User.IdUtente());
            return NoContent();
        }

        [HttpDelete("{id}/mipiace")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TogliMiPiace(int id)
        {
            var commento = await _commentiService.GetCommentoById(id);
            if (commento == null) return NotFound();
            await _commentiService.TogliMiPiace(commento, User.IdUtente());
            return NoContent();
        }
    }
}
