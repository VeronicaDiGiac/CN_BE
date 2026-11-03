using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    // Episodi visti delle serie TV. Ogni modifica restituisce l'avanzamento aggiornato,
    // così il FE ridisegna la scheda senza una seconda richiesta.
    [ApiController]
    [Route("api/episodi")]
    public class EpisodiController : ControllerBase
    {
        private readonly EpisodiService _episodiService;
        private readonly CalendarioSerieService _calendario;

        public EpisodiController(EpisodiService episodiService, CalendarioSerieService calendario)
        {
            _episodiService = episodiService;
            _calendario = calendario;
        }

        // Puntate uscite nell'ultima settimana e in arrivo delle serie seguite (iniziate o "Da vedere")
        [HttpGet("calendario")]
        [Authorize]
        [ProducesResponseType(typeof(List<PuntataCalendarioDto>), 200)]
        public async Task<IActionResult> Calendario() =>
            Ok(await _calendario.Calendario(User.IdUtente()));

        // Admin: crea subito le notifiche dei nuovi episodi (di solito lo fa il controllo
        // periodico di NuoviEpisodiWorker). Restituisce quante ne ha create.
        [HttpPost("notifica-nuovi")]
        [Authorize(Roles = Ruoli.Admin)]
        [ProducesResponseType(typeof(int), 200)]
        public async Task<IActionResult> NotificaNuovi() =>
            Ok(await _calendario.NotificaNuoviEpisodi());

        // Avanzamento dell'utente autenticato in una serie
        [HttpGet("serie/{idFilm}")]
        [Authorize]
        [ProducesResponseType(typeof(AvanzamentoSerieDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Avanzamento(int idFilm)
        {
            var avanzamento = await _episodiService.Avanzamento(idFilm, User.IdUtente());
            return avanzamento == null ? NotFound("Serie non trovata") : Ok(avanzamento);
        }

        // Serie iniziate da un utente (pubblico, come il diario)
        [HttpGet("utente/{idUtente}")]
        [ProducesResponseType(typeof(List<SerieUtenteDto>), 200)]
        public async Task<IActionResult> SerieUtente(int idUtente) =>
            Ok(await _episodiService.SerieUtente(idUtente));

        // PUT perché è idempotente: segnare due volte lo stesso episodio non cambia niente
        [HttpPut("serie/{idFilm}/{stagione}/{episodio}")]
        [Authorize]
        [ProducesResponseType(typeof(AvanzamentoSerieDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Segna(int idFilm, int stagione, int episodio) =>
            await Risposta(idFilm, await _episodiService.Segna(idFilm, User.IdUtente(), stagione, episodio));

        [HttpDelete("serie/{idFilm}/{stagione}/{episodio}")]
        [Authorize]
        [ProducesResponseType(typeof(AvanzamentoSerieDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Togli(int idFilm, int stagione, int episodio)
        {
            await _episodiService.Togli(idFilm, User.IdUtente(), stagione, episodio);
            return await Risposta(idFilm, EsitoEpisodi.Ok);
        }

        // Tutti gli episodi già usciti della stagione
        [HttpPut("serie/{idFilm}/stagioni/{stagione}")]
        [Authorize]
        [ProducesResponseType(typeof(AvanzamentoSerieDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SegnaStagione(int idFilm, int stagione) =>
            await Risposta(idFilm, await _episodiService.SegnaStagione(idFilm, User.IdUtente(), stagione));

        [HttpDelete("serie/{idFilm}/stagioni/{stagione}")]
        [Authorize]
        [ProducesResponseType(typeof(AvanzamentoSerieDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TogliStagione(int idFilm, int stagione)
        {
            await _episodiService.TogliStagione(idFilm, User.IdUtente(), stagione);
            return await Risposta(idFilm, EsitoEpisodi.Ok);
        }

        // L'episodio e tutti quelli usciti prima ("ho visto fino a qui")
        [HttpPut("serie/{idFilm}/fino-a/{stagione}/{episodio}")]
        [Authorize]
        [ProducesResponseType(typeof(AvanzamentoSerieDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SegnaFinoA(int idFilm, int stagione, int episodio)
        {
            if (stagione < 1) return BadRequest("\"Fino a qui\" vale per le stagioni, non per gli episodi speciali");
            return await Risposta(idFilm, await _episodiService.SegnaFinoA(idFilm, User.IdUtente(), stagione, episodio));
        }

        private async Task<IActionResult> Risposta(int idFilm, EsitoEpisodi esito)
        {
            if (esito == EsitoEpisodi.NonTrovato) return NotFound("Serie o episodio non trovato");
            if (esito == EsitoEpisodi.NonUscito) return BadRequest("L'episodio non è ancora uscito");
            var avanzamento = await _episodiService.Avanzamento(idFilm, User.IdUtente());
            return avanzamento == null ? NotFound("Serie non trovata") : Ok(avanzamento);
        }
    }
}
