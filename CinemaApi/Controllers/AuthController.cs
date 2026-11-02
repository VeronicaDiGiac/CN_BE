using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        public AuthController(AuthService authService) { _authService = authService; }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var risultato = await _authService.Login(dto);
                // QUI: Unauthorized con ErrorResponseDto
                // (StatusCode = 401, Messaggio = "Email o password non corretti")
                if (risultato == null) return Unauthorized();
                return Ok(risultato);
            }
            catch (Exception ex)
            {
                // QUI: StatusCode 500 con ErrorResponseDto
                // (Messaggio generico per l'utente, ex.Message eventualmente in Dettaglio)
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var risultato = await _authService.Register(dto);
                // QUI: BadRequest con ErrorResponseDto
                // (StatusCode = 400, Messaggio = "Email già registrata")
                if (risultato == null) return BadRequest("Email già registrata");
                return Ok(risultato);
            }
            catch (Exception ex)
            {
                // QUI: StatusCode 500 con ErrorResponseDto
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("password")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CambiaPassword([FromBody] CambiaPasswordDto dto)
        {
            try
            {
                // Si può cambiare solo la PROPRIA password: l'id arriva dal token
                var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (dto.NuovaPassword == dto.PasswordAttuale)
                    return BadRequest("La nuova password deve essere diversa da quella attuale");

                var risultato = await _authService.CambiaPassword(idUtenteAutenticato, dto);
                if (risultato == null) return NotFound();
                // 400 e non 401: l'utente è autenticato, ha solo sbagliato la password attuale
                if (risultato == false) return BadRequest("La password attuale non è corretta");
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}