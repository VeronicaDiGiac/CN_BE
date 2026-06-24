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
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateFollowDto dto)
        {
            try
            {
                var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var follow = await _followService.CreateFollow(dto, idUtenteAutenticato);
                if (follow == null) return Conflict("Follow già creato");
                return CreatedAtAction(nameof(Create), follow);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet("followers/{idUtente}")]
        [ProducesResponseType(typeof(List<object>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFollowers(int idUtente)
        {
            try
            {
                var follower = await _followService.GetFollowers(idUtente);
                return Ok(follower);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("following/{idUtente}")]
        [ProducesResponseType(typeof(List<object>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFollowing(int idUtente)
        {
            try
            {
                var following = await _followService.GetFollowing(idUtente);
                return Ok(following);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{followingId}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int followingId)
        {
            try
            {
                var idUtenteAutenticato = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var eliminato = await _followService.DeleteFollow(idUtenteAutenticato, followingId);
                if (!eliminato) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }



    }
}