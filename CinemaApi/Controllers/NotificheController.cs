using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    // Ognuno vede solo le proprie notifiche: l'utente arriva sempre dal token
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificheController : ControllerBase
    {
        private const int PageSizeMassimo = 50;

        private readonly NotificheService _notificheService;
        public NotificheController(NotificheService notificheService) { _notificheService = notificheService; }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<NotificaDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Get(int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1 || pageSize < 1 || pageSize > PageSizeMassimo)
                return BadRequest("Paginazione non valida");
            return Ok(await _notificheService.GetNotifiche(User.IdUtente(), pageNumber, pageSize));
        }

        // Chiamato spesso dal FE (campanella): risposta minima
        [HttpGet("non-lette")]
        [ProducesResponseType(typeof(int), 200)]
        public async Task<IActionResult> ContaNonLette() =>
            Ok(await _notificheService.ContaNonLette(User.IdUtente()));

        // Segna lette le notifiche di ciò che l'utente ha appena visto (almeno uno dei parametri):
        // la notifica cliccata, la scheda di un film, una discussione o il profilo di un nuovo follower
        [HttpPut("viste")]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SegnaViste(int? idNotifica, int? idFilm, int? idDiscussione, int? idProfilo)
        {
            if (idNotifica == null && idFilm == null && idDiscussione == null && idProfilo == null)
                return BadRequest("Indica la notifica, il film, la discussione o il profilo visto");
            return Ok(await _notificheService.SegnaViste(User.IdUtente(), idNotifica, idFilm, idDiscussione, idProfilo));
        }

        [HttpPut("lette")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> SegnaTutteLette()
        {
            await _notificheService.SegnaTutteLette(User.IdUtente());
            return NoContent();
        }
    }
}
