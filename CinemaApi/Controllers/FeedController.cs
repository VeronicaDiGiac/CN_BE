using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private const int PageSizeMassimo = 50;

        private readonly FeedService _feedService;
        public FeedController(FeedService feedService) { _feedService = feedService; }

        // Attività recenti delle persone che l'utente autenticato segue
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(PagedResultDto<FeedVoceDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Get(int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1 || pageSize < 1 || pageSize > PageSizeMassimo)
                return BadRequest("Paginazione non valida");
            return Ok(await _feedService.GetFeed(User.IdUtente(), pageNumber, pageSize));
        }
    }
}
