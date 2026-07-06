using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Services;
using Microsoft.AspNetCore.Mvc;

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
    }
}