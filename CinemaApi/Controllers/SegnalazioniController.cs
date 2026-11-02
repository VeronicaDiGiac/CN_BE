using CinemaApi.Helpers;
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
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateSegnalazioneDto dto)
        {
            var idUtenteAutenticato = User.IdUtente();
            if (!await _segnalazioniService.ContenutoEsiste(dto.TipoContenuto, dto.IdContenuto)) return NotFound("Contenuto segnalato non trovato");
            var segnalazione = await _segnalazioniService.CreateSegnalazione(dto, idUtenteAutenticato);
            return CreatedAtAction(nameof(Create), segnalazione);
        }
        [HttpGet]
        [Authorize(Roles = Ruoli.Admin)]
        [ProducesResponseType(typeof(List<object>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Get()
        {
            var segnalazioni = await _segnalazioniService.GetSegnalazioni();
            return Ok(segnalazioni);
        }

        [HttpPut("{id}/stato")]
        [Authorize(Roles = Ruoli.Admin)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateStato(int id, [FromBody] UpdateStatoSegnalazioneDto dto)
        {
            var idAdmin = User.IdUtente();
            var aggiornata = await _segnalazioniService.UpdateStato(id, dto, idAdmin);
            if (!aggiornata) return NotFound();
            return NoContent();
        }


    }
}