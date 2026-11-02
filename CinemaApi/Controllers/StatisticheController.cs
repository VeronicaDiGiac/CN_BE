using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    // Statistiche pubbliche come il diario da cui sono calcolate
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticheController : ControllerBase
    {
        private readonly StatisticheService _statisticheService;
        public StatisticheController(StatisticheService statisticheService) { _statisticheService = statisticheService; }

        // anno: riepilogo di quell'anno; senza anno: tutto il diario
        [HttpGet("utente/{idUtente}")]
        [ProducesResponseType(typeof(StatisticheDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(int idUtente, int? anno)
        {
            if (anno is < 1888 or > 2100) return BadRequest("Anno non valido");
            if (!await _statisticheService.UtenteEsiste(idUtente)) return NotFound();
            return Ok(await _statisticheService.GetStatistiche(idUtente, anno));
        }
    }
}
