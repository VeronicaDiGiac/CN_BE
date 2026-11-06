using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    // Recensioni e discussioni salvate ("Salvati"). Ognuno vede e modifica solo i propri: l'utente arriva dal token.
    // tipo nell'indirizzo: recensione o discussione
    [ApiController]
    [Route("api/salvati")]
    [Authorize]
    public class SalvatiController : ControllerBase
    {
        private const int PageSizeMassimo = 50;
        private const string TipoNonValido = "Tipo non valido: usa recensione o discussione";
        private readonly SalvatiService _salvati;
        public SalvatiController(SalvatiService salvati) { _salvati = salvati; }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<SalvatoDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Get(string? tipo, int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1 || pageSize is < 1 or > PageSizeMassimo) return BadRequest("Pagina non valida (pageSize da 1 a 50)");
            string? normalizzato = null;
            if (tipo != null && (normalizzato = SalvatiService.Normalizza(tipo)) == null) return BadRequest(TipoNonValido);
            return Ok(await _salvati.GetSalvati(User.IdUtente(), normalizzato, pageNumber, pageSize));
        }

        // Tutto quello che l'utente ha salvato, per accendere i segnalibri nelle pagine
        [HttpGet("chiavi")]
        [ProducesResponseType(typeof(List<ChiaveSalvatoDto>), 200)]
        public async Task<IActionResult> GetChiavi() => Ok(await _salvati.GetChiavi(User.IdUtente()));

        // PUT perché è idempotente: ripeterlo non crea doppioni
        [HttpPut("{tipo}/{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Salva(string tipo, int id)
        {
            var t = SalvatiService.Normalizza(tipo);
            if (t == null) return BadRequest(TipoNonValido);
            if (!await _salvati.ContenutoEsiste(t, id)) return NotFound();
            await _salvati.Salva(User.IdUtente(), t, id);
            return NoContent();
        }

        [HttpDelete("{tipo}/{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Togli(string tipo, int id)
        {
            var t = SalvatiService.Normalizza(tipo);
            if (t == null) return BadRequest(TipoNonValido);
            await _salvati.Togli(User.IdUtente(), t, id);
            return NoContent();
        }
    }
}
