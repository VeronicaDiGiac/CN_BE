using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    // Lista "Da vedere". Aggiunta e rimozione lavorano sull'id del film dell'utente autenticato,
    // così il FE può fare da interruttore senza conoscere l'id della riga.
    [ApiController]
    [Route("api/davedere")]
    public class DaVedereController : ControllerBase
    {
        private readonly DaVedereService _daVedereService;
        public DaVedereController(DaVedereService daVedereService) { _daVedereService = daVedereService; }

        [HttpGet("utente/{idUtente}")]
        [ProducesResponseType(typeof(List<DaVedereDto>), 200)]
        public async Task<IActionResult> GetLista(int idUtente) =>
            Ok(await _daVedereService.GetLista(idUtente));

        // PUT perché è idempotente: ripeterlo non crea doppioni
        [HttpPut("{idFilm}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Aggiungi(int idFilm)
        {
            if (!await _daVedereService.FilmEsiste(idFilm)) return NotFound("Film non trovato");
            await _daVedereService.Aggiungi(User.IdUtente(), idFilm);
            return NoContent();
        }

        [HttpDelete("{idFilm}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Rimuovi(int idFilm)
        {
            if (!await _daVedereService.Rimuovi(User.IdUtente(), idFilm)) return NotFound();
            return NoContent();
        }
    }
}
