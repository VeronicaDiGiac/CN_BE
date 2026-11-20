using CinemaApi.Helpers;
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
    public class FollowController : ControllerBase
    {
        private readonly FollowService _followService;
        public FollowController(FollowService followService) { _followService = followService; }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(FollowDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateFollowDto dto)
        {
            var idUtenteAutenticato = User.IdUtente();
            if (dto.FollowingId == idUtenteAutenticato) return BadRequest("Non puoi seguire te stesso");
            if (!await _followService.UtenteEsiste(dto.FollowingId)) return NotFound("Utente non trovato");
            var follow = await _followService.CreateFollow(dto, idUtenteAutenticato);
            if (follow == null) return Conflict("Follow già creato");
            return CreatedAtAction(nameof(Create), follow);
        }
        [HttpGet("followers/{idUtente}")]
        [ProducesResponseType(typeof(List<FollowDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFollowers(int idUtente)
        {
            var follower = await _followService.GetFollowers(idUtente);
            return Ok(follower);
        }

        [HttpGet("following/{idUtente}")]
        [ProducesResponseType(typeof(List<FollowDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFollowing(int idUtente)
        {
            var following = await _followService.GetFollowing(idUtente);
            return Ok(following);
        }

        // Dal profilo di una persona: la segui e hai la campanella accesa?
        [HttpGet("{followingId}/avvisi")]
        [Authorize]
        [ProducesResponseType(typeof(AvvisiFollowDto), 200)]
        public async Task<IActionResult> StatoAvvisi(int followingId) =>
            Ok(await _followService.StatoAvvisi(User.IdUtente(), followingId));

        // Campanella: avvisi quando la persona seguita guarda, vede, vota o recensisce. 404 se non la segui
        [HttpPut("{followingId}/avvisi")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ImpostaAvvisi(int followingId, [FromBody] AvvisiFollowDto dto)
        {
            if (!await _followService.ImpostaAvvisi(User.IdUtente(), followingId, dto.Avvisi))
                return NotFound("Non segui questo utente");
            return NoContent();
        }

        [HttpDelete("{followingId}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int followingId)
        {
            var idUtenteAutenticato = User.IdUtente();
            var eliminato = await _followService.DeleteFollow(idUtenteAutenticato, followingId);
            if (!eliminato) return NotFound();
            return NoContent();
        }



    }
}