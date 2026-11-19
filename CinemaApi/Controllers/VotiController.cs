using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    // Stelle a film e recensioni. PUT perché è idempotente: ripeterlo cambia il voto, non ne aggiunge un altro.
    // Ogni risposta ha media, numero dei voti e il voto dell'utente, già aggiornati.
    [ApiController]
    [Authorize]
    public class VotiController : ControllerBase
    {
        private readonly VotiService _voti;

        public VotiController(VotiService voti) { _voti = voti; }

        [HttpPut("api/film/{idFilm}/voto")]
        [ProducesResponseType(typeof(VotiDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> VotaFilm(int idFilm, [FromBody] VotoDto dto)
        {
            if (!await _voti.FilmEsiste(idFilm)) return NotFound("Film non trovato");
            return Ok(await _voti.VotaFilm(idFilm, User.IdUtente(), dto.Voto));
        }

        [HttpDelete("api/film/{idFilm}/voto")]
        [ProducesResponseType(typeof(VotiDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TogliVotoFilm(int idFilm)
        {
            if (!await _voti.FilmEsiste(idFilm)) return NotFound("Film non trovato");
            return Ok(await _voti.TogliVotoFilm(idFilm, User.IdUtente()));
        }

        [HttpPut("api/recensioni/{id}/voto")]
        [ProducesResponseType(typeof(VotiDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> VotaRecensione(int id, [FromBody] VotoDto dto)
        {
            var recensione = await _voti.GetRecensione(id);
            if (recensione == null) return NotFound();
            if (recensione.IdUtente == User.IdUtente()) return BadRequest("Non puoi votare la tua recensione");
            return Ok(await _voti.VotaRecensione(id, User.IdUtente(), dto.Voto));
        }

        [HttpDelete("api/recensioni/{id}/voto")]
        [ProducesResponseType(typeof(VotiDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TogliVotoRecensione(int id)
        {
            if (await _voti.GetRecensione(id) == null) return NotFound();
            return Ok(await _voti.TogliVotoRecensione(id, User.IdUtente()));
        }
    }
}
