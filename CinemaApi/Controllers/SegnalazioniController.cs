using Microsoft.AspNetCore.Mvc;
using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CinemaApi.Models;


namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SegnalazioniController : ControllerBase
    {
        private readonly SegnalazioniService _segnalazioniService;
        public SegnalazioniController(SegnalazioniService followService) { _segnalazioniService = followService; }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateSegnalazioneDto dto)
        {
            try
            {
                var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var segnalazione = await _segnalazioniService.CreateSegnalazione(dto, idUtenteAutenticato);
                return CreatedAtAction(nameof(Create), segnalazione);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<object>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Get()
        {
            try
            {
                var segnalazioni = await _segnalazioniService.GetSegnalazioni();
                return Ok(segnalazioni);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}/stato")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateStato(int id, [FromBody] UpdateStatoSegnalazioneDto dto)
        {
            try
            {
                var idAdmin = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var aggiornata = await _segnalazioniService.UpdateStato(id, dto, idAdmin);
                if (!aggiornata) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


    }
}