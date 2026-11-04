using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    // Pannello di controllo: solo per l'admin.
    // Recensioni, discussioni e commenti si eliminano con i loro endpoint (DELETE /api/recensioni/{id}…).
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = Ruoli.Admin)]
    public class AdminController : ControllerBase
    {
        private const int PageSizeMassimo = 50;
        private const string MessaggioPagina = "Pagina non valida (pageSize da 1 a 50)";
        private readonly AdminService _admin;
        public AdminController(AdminService admin) { _admin = admin; }

        // dal: da quando contare le novità (es. ultima visita del pannello); predefinito: ultimi 7 giorni
        [HttpGet("riepilogo")]
        [ProducesResponseType(typeof(RiepilogoAdminDto), 200)]
        public async Task<IActionResult> GetRiepilogo(DateTime? dal)
        {
            var inizio = dal ?? DateTime.Now.AddDays(-7);
            return Ok(await _admin.GetRiepilogo(inizio));
        }

        // tipo: Iscrizione, Recensione, Discussione o Commento (vuoto = tutti)
        [HttpGet("attivita")]
        [ProducesResponseType(typeof(List<AttivitaAdminDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetAttivita(string? tipo, int limite = 30)
        {
            if (tipo != null && tipo is not (TipiAttivitaAdmin.Iscrizione or TipiAttivitaAdmin.Recensione
                or TipiAttivitaAdmin.Discussione or TipiAttivitaAdmin.Commento))
                return BadRequest("Tipo non valido: usa Iscrizione, Recensione, Discussione o Commento");
            return Ok(await _admin.GetAttivita(Math.Clamp(limite, 1, 100), tipo));
        }

        [HttpGet("utenti")]
        [ProducesResponseType(typeof(PagedResultDto<UtenteAdminDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetUtenti(string? q, int pageNumber = 1, int pageSize = 20)
        {
            if (!PaginaValida(pageNumber, pageSize)) return BadRequest(MessaggioPagina);
            return Ok(await _admin.GetUtenti(q, pageNumber, pageSize));
        }

        // Elimina l'utente e tutto quello che ha scritto. Non si eliminano se stessi né un altro admin.
        [HttpDelete("utenti/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> EliminaUtente(int id)
        {
            if (id == User.IdUtente()) return BadRequest("Non puoi eliminare il tuo account da qui: usa il tuo profilo");
            var utente = await _admin.GetUtente(id);
            if (utente == null) return NotFound();
            if (utente.Ruolo == Ruoli.Admin) return BadRequest("Non si può eliminare un altro admin");
            await _admin.EliminaUtente(id);
            return NoContent();
        }

        [HttpGet("recensioni")]
        [ProducesResponseType(typeof(PagedResultDto<RecensioneAdminDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetRecensioni(string? q, bool segnalate = false, int pageNumber = 1, int pageSize = 20)
        {
            if (!PaginaValida(pageNumber, pageSize)) return BadRequest(MessaggioPagina);
            return Ok(await _admin.GetRecensioni(q, segnalate, pageNumber, pageSize));
        }

        [HttpGet("discussioni")]
        [ProducesResponseType(typeof(PagedResultDto<DiscussioneAdminDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetDiscussioni(string? q, bool segnalate = false, int pageNumber = 1, int pageSize = 20)
        {
            if (!PaginaValida(pageNumber, pageSize)) return BadRequest(MessaggioPagina);
            return Ok(await _admin.GetDiscussioni(q, segnalate, pageNumber, pageSize));
        }

        [HttpGet("commenti")]
        [ProducesResponseType(typeof(PagedResultDto<CommentoAdminDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetCommenti(string? q, bool segnalate = false, int pageNumber = 1, int pageSize = 20)
        {
            if (!PaginaValida(pageNumber, pageSize)) return BadRequest(MessaggioPagina);
            return Ok(await _admin.GetCommenti(q, segnalate, pageNumber, pageSize));
        }

        private static bool PaginaValida(int pageNumber, int pageSize) =>
            pageNumber >= 1 && pageSize is >= 1 and <= PageSizeMassimo;
    }
}
