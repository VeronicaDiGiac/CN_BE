using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CinemaApi.Controllers
{
    // Sezioni della home. Personali: "Continua a guardare" e "Da vedere, ora in streaming".
    // Per tutti: classifica, curiosità del giorno, discussioni animate, sondaggio e film club.
    [ApiController]
    [Route("api/home")]
    public class HomeController : ControllerBase
    {
        private readonly HomeService _home;
        private readonly SondaggiService _sondaggi;
        private readonly FilmClubService _filmClub;

        public HomeController(HomeService home, SondaggiService sondaggi, FilmClubService filmClub)
        {
            _home = home;
            _sondaggi = sondaggi;
            _filmClub = filmClub;
        }

        [HttpGet("continua")]
        [Authorize]
        [EnableRateLimiting(TmdbController.LimiteTmdb)]
        [ProducesResponseType(typeof(List<ContinuaGuardareDto>), 200)]
        public async Task<IActionResult> Continua() => Ok(await _home.ContinuaAGuardare(User.IdUtente()));

        [HttpGet("da-vedere-streaming")]
        [Authorize]
        [EnableRateLimiting(TmdbController.LimiteTmdb)]
        [ProducesResponseType(typeof(List<DaVedereInStreamingDto>), 200)]
        public async Task<IActionResult> DaVedereInStreaming() => Ok(await _home.DaVedereInStreaming(User.IdUtente()));

        // giorni: 7 o 30
        [HttpGet("classifica")]
        [ProducesResponseType(typeof(ClassificaDto), 200)]
        public async Task<IActionResult> Classifica(int giorni = 7) =>
            Ok(await _home.Classifica(giorni == 30 ? 30 : 7));

        // 204 se oggi non c'è niente da mostrare
        [HttpGet("curiosita")]
        [EnableRateLimiting(TmdbController.LimiteTmdb)]
        [ProducesResponseType(typeof(CuriositaGiornoDto), 200)]
        [ProducesResponseType(204)]
        public async Task<IActionResult> CuriositaDelGiorno()
        {
            var curiosita = await _home.CuriositaDelGiorno();
            return curiosita == null ? NoContent() : Ok(curiosita);
        }

        [HttpGet("discussioni")]
        [ProducesResponseType(typeof(List<DiscussioneAnimataDto>), 200)]
        public async Task<IActionResult> Discussioni() => Ok(await _home.DiscussioniAnimate());

        // ---- Sondaggio della settimana ----

        // 204 se non c'è nessun sondaggio iniziato
        [HttpGet("sondaggio")]
        [ProducesResponseType(typeof(SondaggioDto), 200)]
        [ProducesResponseType(204)]
        public async Task<IActionResult> Sondaggio()
        {
            var sondaggio = await _sondaggi.Corrente(User.IdUtenteOpzionale());
            return sondaggio == null ? NoContent() : Ok(sondaggio);
        }

        // PUT: rivotare cambia la risposta. Restituisce il sondaggio con i risultati aggiornati
        [HttpPut("sondaggio/{id}/voto")]
        [Authorize]
        [ProducesResponseType(typeof(SondaggioDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Vota(int id, [FromBody] VotoSondaggioDto dto)
        {
            var idUtente = User.IdUtente();
            return await _sondaggi.Vota(id, idUtente, dto.IdOpzione) switch
            {
                SondaggiService.EsitoVoto.NonTrovato => NotFound("Sondaggio non trovato"),
                SondaggiService.EsitoVoto.NonIniziato => BadRequest("Il sondaggio non è ancora iniziato"),
                SondaggiService.EsitoVoto.OpzioneNonValida => BadRequest("Risposta non valida"),
                _ => Ok(await _sondaggi.Dettaglio(id, idUtente))
            };
        }

        [HttpDelete("sondaggio/{id}/voto")]
        [Authorize]
        [ProducesResponseType(typeof(SondaggioDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TogliVoto(int id)
        {
            var idUtente = User.IdUtente();
            if (!await _sondaggi.TogliVoto(id, idUtente)) return NotFound("Sondaggio non trovato");
            return Ok(await _sondaggi.Dettaglio(id, idUtente));
        }

        // ---- Film club ----

        [HttpGet("film-club")]
        [ProducesResponseType(typeof(FilmClubHomeDto), 200)]
        public async Task<IActionResult> FilmClub() => Ok(await _filmClub.Home(User.IdUtenteOpzionale()));
    }

    // Pannello admin: sondaggi e film club
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = Ruoli.Admin)]
    public class AdminHomeController : ControllerBase
    {
        private readonly SondaggiService _sondaggi;
        private readonly FilmClubService _filmClub;

        public AdminHomeController(SondaggiService sondaggi, FilmClubService filmClub)
        {
            _sondaggi = sondaggi;
            _filmClub = filmClub;
        }

        [HttpGet("sondaggi")]
        [ProducesResponseType(typeof(List<SondaggioAdminDto>), 200)]
        public async Task<IActionResult> Sondaggi() => Ok(await _sondaggi.Elenco());

        [HttpPost("sondaggi")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreaSondaggio([FromBody] CreaSondaggioDto dto)
        {
            var id = await _sondaggi.Crea(dto);
            if (id == null) return BadRequest("Uno dei titoli scelti non è nel catalogo");
            return StatusCode(201, new { idSondaggio = id });
        }

        [HttpDelete("sondaggi/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> EliminaSondaggio(int id) =>
            await _sondaggi.Elimina(id) ? NoContent() : NotFound();

        [HttpGet("film-club")]
        [ProducesResponseType(typeof(List<FilmClubDto>), 200)]
        public async Task<IActionResult> FilmClub() => Ok(await _filmClub.Programma());

        [HttpPut("film-club")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ImpostaFilmClub([FromBody] ImpostaFilmClubDto dto) =>
            await _filmClub.Imposta(dto.IdFilm, dto.ProssimaSettimana) ? NoContent() : BadRequest("Titolo non trovato nel catalogo");
    }
}
