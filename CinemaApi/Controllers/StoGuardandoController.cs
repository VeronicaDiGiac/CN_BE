using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    // "Sto guardando" in stile MSN: lo accende l'utente, lo vedono solo lui e chi lo segue
    [ApiController]
    [Route("api/sto-guardando")]
    [Authorize]
    public class StoGuardandoController : ControllerBase
    {
        private readonly StoGuardandoService _service;

        public StoGuardandoController(StoGuardandoService service) { _service = service; }

        // Accende (o rinnova) lo stato su un film o una serie. Avvisa chi lo segue con la campanella accesa
        [HttpPut("{idFilm}")]
        [ProducesResponseType(typeof(StaGuardandoDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Inizia(int idFilm)
        {
            var stato = await _service.Inizia(User.IdUtente(), idFilm);
            return stato == null ? NotFound("Film non trovato") : Ok(stato);
        }

        [HttpDelete]
        [ProducesResponseType(204)]
        public async Task<IActionResult> Smetti()
        {
            await _service.Smetti(User.IdUtente());
            return NoContent();
        }

        // Le persone seguite che stanno guardando qualcosa adesso (pagina Attività)
        [HttpGet("seguiti")]
        [ProducesResponseType(typeof(List<StaGuardandoDto>), 200)]
        public async Task<IActionResult> Seguiti() => Ok(await _service.Seguiti(User.IdUtente()));

        // Cosa sta guardando un utente: 204 se niente o se chi chiede non lo segue
        [HttpGet("utente/{idUtente}")]
        [ProducesResponseType(typeof(StaGuardandoDto), 200)]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DiUtente(int idUtente)
        {
            var stato = await _service.PerProfilo(idUtente, User.IdUtente());
            return stato == null ? NoContent() : Ok(stato);
        }
    }
}
