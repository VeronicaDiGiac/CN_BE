using CinemaApi.Helpers;
using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // Nome della policy di rate limiting registrata in Program.cs
        // Policy di rate limiting per login, registrazione e accesso con Google (Program.cs)
        public const string LimiteAccesso = "accesso";

        public const string LimitePasswordDimenticata = "password-dimenticata";

        private readonly AuthService _authService;
        private readonly VerificaAuth0 _verificaAuth0;

        public AuthController(AuthService authService, VerificaAuth0 verificaAuth0)
        {
            _authService = authService;
            _verificaAuth0 = verificaAuth0;
        }

        // Accesso con Google: il FE manda l'ID token ricevuto da Auth0 e riceve la solita sessione Cineverse
        [HttpPost("esterno")]
        [EnableRateLimiting(LimiteAccesso)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> AccessoEsterno([FromBody] AccessoEsternoDto dto)
        {
            if (!_verificaAuth0.Configurato) return StatusCode(503, "Accesso con Google non configurato");
            var esterno = await _verificaAuth0.Verifica(dto.IdToken);
            if (esterno == null) return Unauthorized();

            var risultato = await _authService.AccediConAuth0(esterno);
            if (risultato == null) return BadRequest("L'account Google non ha un'email verificata");
            return Ok(risultato);
        }

        [HttpPost("login")]
        [EnableRateLimiting(LimiteAccesso)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var risultato = await _authService.Login(dto);
            // QUI: Unauthorized con ErrorResponseDto
            // (StatusCode = 401, Messaggio = "Email o password non corretti")
            if (risultato == null) return Unauthorized();
            return Ok(risultato);
        }

        [HttpPost("register")]
        [EnableRateLimiting(LimiteAccesso)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var risultato = await _authService.Register(dto);
            // QUI: BadRequest con ErrorResponseDto
            // (StatusCode = 400, Messaggio = "Email già registrata")
            if (risultato == null) return BadRequest("Email già registrata");
            return Ok(risultato);
        }

        [HttpPut("password")]
        [Authorize]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CambiaPassword([FromBody] CambiaPasswordDto dto)
        {
            // Si può cambiare solo la PROPRIA password: l'id arriva dal token
            var idUtenteAutenticato = User.IdUtente();
            if (dto.NuovaPassword == dto.PasswordAttuale)
                return BadRequest("La nuova password deve essere diversa da quella attuale");

            var risultato = await _authService.CambiaPassword(idUtenteAutenticato, dto);
            if (risultato == null) return NotFound();
            // 400 e non 401: l'utente è autenticato, ha solo sbagliato la password attuale
            if (risultato == false) return BadRequest("La password attuale non è corretta");

            // Il cambio password invalida tutti i token, compreso quello usato per questa richiesta:
            // se ne restituisce uno nuovo per restare collegati su questo dispositivo
            var sessione = await _authService.CreaSessione(idUtenteAutenticato);
            return Ok(sessione);
        }

        [HttpPost("password-dimenticata")]
        [EnableRateLimiting(LimitePasswordDimenticata)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(429)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PasswordDimenticata([FromBody] PasswordDimenticataDto dto)
        {
            await _authService.PasswordDimenticata(dto);
            // Stessa risposta sia se l'email esiste sia se non esiste
            // (il messaggio "Se l'email è registrata…" lo mostra il FE)
            return NoContent();
        }

        [HttpPost("reimposta-password")]
        [EnableRateLimiting(LimitePasswordDimenticata)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(429)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ReimpostaPassword([FromBody] ReimpostaPasswordDto dto)
        {
            var risultato = await _authService.ReimpostaPassword(dto);
            if (!risultato) return BadRequest("Il link non è valido o è scaduto. Richiedine uno nuovo");
            return NoContent();
        }
    }
}