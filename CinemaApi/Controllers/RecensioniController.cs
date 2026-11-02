using CinemaApi.DTOs.RequestDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
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
            var recensioni = await _recensioniService.GetRecensioni(idFilm, idUtente, User.IdUtenteOpzionale());
            return Ok(recensioni);
        }

        // PUT perché è idempotente: ripeterlo non aggiunge un secondo "mi piace"
        [HttpPut("{id}/mipiace")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MettiMiPiace(int id)
        {
            var recensione = await _recensioniService.GetRecensioneById(id);
            if (recensione == null) return NotFound();
            if (recensione.IdUtente == User.IdUtente()) return BadRequest("Non puoi mettere \"mi piace\" alla tua recensione");

            await _recensioniService.MettiMiPiace(id, User.IdUtente());
            return NoContent();
        }

        [HttpDelete("{id}/mipiace")]
        [Authorize]
        [ProducesResponseType(204)]
        public async Task<IActionResult> TogliMiPiace(int id)
        {
            await _recensioniService.TogliMiPiace(id, User.IdUtente());
            return NoContent();
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateRecensioneDto dto)
        {
            var idUtenteAutenticato = User.IdUtente();
            if (!await _recensioniService.FilmEsiste(dto.IdFilm)) return NotFound("Film non trovato");
            var recensione = await _recensioniService.CreateRecensione(dto, idUtenteAutenticato);
            if (recensione == null) return Conflict("Hai già lasciato una recensione per questo film");
            return CreatedAtAction(nameof(Create), recensione);
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Update(int id, [FromBody] CreateRecensioneDto dto)
        {
            var idUtenteAutenticato = User.IdUtente();
            var ruolo = User.Ruolo(); 
            var recensione = await _recensioniService.GetRecensioneById(id);
            if (recensione == null) return NotFound();
            //Sostituisco il forbidden con il controllo Admin che ho inserito negli helpers
            //if (recensione.IdUtente != idUtenteAutenticato) return Forbid();
            if (!AutorizzazioneHelper.PuoModificare(recensione.IdUtente, idUtenteAutenticato, ruolo)) return Forbid();
            await _recensioniService.UpdateRecensione(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            var idUtenteAutenticato = User.IdUtente();
            var ruolo = User.Ruolo();
            var recensione = await _recensioniService.GetRecensioneById(id);
            if (recensione == null) return NotFound();
            //if (recensione.IdUtente != idUtenteAutenticato) return Forbid();
            if (!AutorizzazioneHelper.PuoModificare(recensione.IdUtente, idUtenteAutenticato, ruolo)) return Forbid();
            var eliminata = await _recensioniService.DeleteRecensione(id);
            if (!eliminata) return NotFound();
            return NoContent();
        }
    }
}