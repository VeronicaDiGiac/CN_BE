using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    // Discussioni su film e serie. I messaggi si leggono e scrivono con /api/commenti?idDiscussione=
    [ApiController]
    [Route("api/discussioni")]
    public class DiscussioniController : ControllerBase
    {
        private readonly DiscussioniService _discussioni;
        public DiscussioniController(DiscussioniService discussioni) { _discussioni = discussioni; }

        // Pubblico. stagione+episodio: solo quell'episodio. ordinamento: recenti (predefinito) o apprezzate
        [HttpGet("film/{idFilm}")]
        [ProducesResponseType(typeof(List<DiscussioneDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetDiscussioni(int idFilm, int? stagione, int? episodio, string? ordinamento)
        {
            if ((stagione == null) != (episodio == null)) return BadRequest("Indica sia la stagione sia l'episodio");
            return Ok(await _discussioni.GetDiscussioni(idFilm, stagione, episodio, ordinamento, User.IdUtenteOpzionale()));
        }

        // Commenti di un episodio: il filo, se qualcuno ha già commentato (altrimenti 204).
        // I commenti si leggono e scrivono con /api/commenti?idDiscussione=
        [HttpGet("episodio")]
        [ProducesResponseType(typeof(DiscussioneDto), 200)]
        [ProducesResponseType(204)]
        public async Task<IActionResult> GetFiloEpisodio(int idFilm, int stagione, int episodio)
        {
            var filo = await _discussioni.GetFiloEpisodio(idFilm, stagione, episodio, User.IdUtenteOpzionale());
            return filo == null ? NoContent() : Ok(filo);
        }

        // Il filo dell'episodio, creato al primo commento: il FE lo chiama prima di scrivere il commento
        [HttpPost("episodio")]
        [Authorize]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> FiloEpisodio([FromBody] FiloEpisodioDto dto)
        {
            if (!await _discussioni.FilmEsiste(dto.IdFilm)) return NotFound("Serie non trovata");
            if (!await _discussioni.ESerie(dto.IdFilm)) return BadRequest("Solo le serie hanno episodi");
            return Ok(await _discussioni.FiloEpisodio(dto.IdFilm, dto.Stagione, dto.Episodio, User.IdUtente()));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DiscussioneDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetDiscussione(int id)
        {
            var discussione = await _discussioni.GetDiscussione(id, User.IdUtenteOpzionale());
            return discussione == null ? NotFound() : Ok(discussione);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(DiscussioneDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Crea([FromBody] SalvaDiscussioneDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Titolo)) return BadRequest("Il titolo è obbligatorio");
            if (!await _discussioni.FilmEsiste(dto.IdFilm)) return NotFound("Film non trovato");
            if ((dto.Stagione == null) != (dto.Episodio == null)) return BadRequest("Indica sia la stagione sia l'episodio");
            if (dto.Stagione != null && !await _discussioni.ESerie(dto.IdFilm))
                return BadRequest("Solo le serie hanno episodi");

            var id = await _discussioni.Crea(dto, User.IdUtente());
            return CreatedAtAction(nameof(GetDiscussione), new { id }, await _discussioni.GetDiscussione(id, User.IdUtente()));
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Aggiorna(int id, [FromBody] SalvaDiscussioneDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Titolo)) return BadRequest("Il titolo è obbligatorio");
            var discussione = await _discussioni.GetDiscussioneById(id);
            if (discussione == null) return NotFound();
            if (discussione.FiloEpisodio || !AutorizzazioneHelper.PuoModificare(discussione.IdUtente, User.IdUtente(), User.Ruolo())) return Forbid();
            await _discussioni.Aggiorna(discussione, dto);
            return NoContent();
        }

        // Autore o admin: si porta via tutti i messaggi
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Elimina(int id)
        {
            var discussione = await _discussioni.GetDiscussioneById(id);
            if (discussione == null) return NotFound();
            if (discussione.FiloEpisodio ? User.Ruolo() != Ruoli.Admin
                : !AutorizzazioneHelper.PuoModificare(discussione.IdUtente, User.IdUtente(), User.Ruolo())) return Forbid();
            await _discussioni.Elimina(id);
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
            var discussione = await _discussioni.GetDiscussioneById(id);
            if (discussione == null) return NotFound();
            if (discussione.FiloEpisodio) return BadRequest("Ai commenti di un episodio si mette \"mi piace\" uno per uno");
            if (discussione.IdUtente == User.IdUtente()) return BadRequest("Non puoi mettere \"mi piace\" alla tua discussione");
            await _discussioni.MettiMiPiace(discussione, User.IdUtente());
            return NoContent();
        }

        [HttpDelete("{id}/mipiace")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TogliMiPiace(int id)
        {
            var discussione = await _discussioni.GetDiscussioneById(id);
            if (discussione == null) return NotFound();
            await _discussioni.TogliMiPiace(discussione, User.IdUtente());
            return NoContent();
        }
    }
}
