using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CinemaApi.Controllers
{
    // Curiosità sulla produzione di un film o di una serie del catalogo, da Wikipedia. Pubblica.
    // Stesso limite di richieste di TMDB: dietro ogni richiesta nuova ci sono chiamate a servizi esterni
    [ApiController]
    [EnableRateLimiting(TmdbController.LimiteTmdb)]
    public class CuriositaController : ControllerBase
    {
        private readonly WikipediaService _wikipedia;

        public CuriositaController(WikipediaService wikipedia) { _wikipedia = wikipedia; }

        // 204 se non ci sono curiosità (film non su TMDB, senza pagina Wikipedia o senza sezione sulla produzione)
        [HttpGet("api/film/{idFilm}/curiosita")]
        [ProducesResponseType(typeof(CuriositaDto), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(429)]
        public async Task<IActionResult> Curiosita(int idFilm)
        {
            if (!await _wikipedia.FilmEsiste(idFilm)) return NotFound();
            var curiosita = await _wikipedia.Curiosita(idFilm);
            return curiosita == null ? NoContent() : Ok(curiosita);
        }
    }
}
