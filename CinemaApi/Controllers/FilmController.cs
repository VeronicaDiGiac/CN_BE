using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilmController : ControllerBase
    {
        private readonly FilmService _filmService;
        public FilmController(FilmService filmService) { _filmService = filmService; }

        [HttpGet("{id}/dettaglio")]
        [ProducesResponseType(typeof(CinemaApi.DTOs.ResponseDto.FilmDettaglioDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDettaglio(int id)
        {
            // Autenticazione OPZIONALE: niente [Authorize] qui, l'endpoint
            // resta pubblico. Se però arriva un token valido, lo leggiamo
            // comunque per lo stato personale (GiaRecensito, InDaVedere, visioni);
            // altrimenti resta null e lo stato personale è vuoto.
            var film = await _filmService.GetFilmDettaglio(id, User.IdUtenteOpzionale());
            if (film == null) return NotFound();
            return Ok(film);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<FilmDto>), 200)]
        [ProducesResponseType(500)]
        // ordinamento: recenti (predefinito), voto, titolo (Helpers.OrdinamentiCatalogo)
        // tipo: Film o Serie (Helpers.TipiTitolo); senza, tutti i titoli
        [ProducesResponseType(400)]
        public async Task<IActionResult> Get(string? titolo, string? regista, int? annoUscita, string? genere, string? attori, int pageNumber = 1, int pageSize = 20, string? ordinamento = null, string? tipo = null)
        {
            var tipoTitolo = TipiTitolo.Normalizza(tipo);
            if (tipo != null && tipoTitolo == null) return BadRequest("Tipo non valido: Film o Serie");
            var risultato = await _filmService.GetFilm(titolo, regista, annoUscita, genere, attori, pageNumber, pageSize, ordinamento, tipoTitolo);
            return Ok(risultato);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(FilmDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetById(int id)
        {
            var film = await _filmService.GetFilmApprovato(id);
            if (film == null) return NotFound();
            return Ok(film);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(FilmDto), 201)]
        [ProducesResponseType(typeof(FilmDoppioneDto), 409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateFilmDto dto)
        {
            var idUtenteAutenticato = User.IdUtente();
            var ruolo = User.Ruolo();
            var (film, doppione) = await _filmService.CreateFilm(dto, idUtenteAutenticato, ruolo);
            if (doppione != null)
            {
                return Conflict(new FilmDoppioneDto
                {
                    Messaggio = doppione.Stato == StatiFilm.Approvato
                        ? "Questo film è già nel catalogo"
                        : "Questo film è già stato proposto ed è in attesa di approvazione",
                    IdFilm = doppione.IdFilm,
                    Titolo = doppione.Titolo,
                    AnnoUscita = doppione.AnnoUscita,
                    Stato = doppione.Stato
                });
            }
            return CreatedAtAction(nameof(Create), film);
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Update(int id, [FromBody] CreateFilmDto dto)
        {
            var idUtenteAutenticato = User.IdUtente();
            var ruolo = User.Ruolo();
            var film = await _filmService.GetFilmById(id);
            if (film == null) return NotFound();
            if (!AutorizzazioneHelper.PuoModificareFilm(film, idUtenteAutenticato, ruolo)) return Forbid();
            var aggiornato = await _filmService.UpdateFilm(id, dto);
            if (!aggiornato) return NotFound();
            return NoContent();
        }



        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            var idUtenteAutenticato = User.IdUtente();
            var ruolo = User.Ruolo();
            var film = await _filmService.GetFilmById(id);
            if (film == null) return NotFound();
            if (!AutorizzazioneHelper.PuoModificareFilm(film, idUtenteAutenticato, ruolo)) return Forbid();
            var eliminato = await _filmService.DeleteFilm(id);
            if (!eliminato) return NotFound();
            return NoContent();
        }
    

        [HttpPost("{id}/immagine")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UploadImmagine(int id, IFormFile file)
        {
            if (!FileValidationHelper.EImmagineValida(file, out var errore))
                return BadRequest(errore);
            if (!await FileValidationHelper.HaFirmaBinariaValida(file))
                return BadRequest("Il file non sembra essere un'immagine valida");
            var idUtenteAutenticato = User.IdUtente();
            var ruolo = User.Ruolo();
            var film = await _filmService.GetFilmById(id);
            if (film == null) return NotFound();
            if (!AutorizzazioneHelper.PuoModificareFilm(film, idUtenteAutenticato, ruolo)) return Forbid();
           
            var updateUrlImage = await _filmService.SaveImage(id, file);
            //Capire se lasciarlo il doppio controllo 
            if (updateUrlImage == null) return NotFound();
            return NoContent();
        }

        [HttpPut("{id}/stato")]
        [Authorize(Roles = Ruoli.Admin)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateStatoFilm(int id, [FromBody] UpdateStatoFilmDto dto)
        {
            var aggiornato = await _filmService.UpdateStatoFilm(id, dto.Stato);
            if (!aggiornato) return NotFound();
            return NoContent();
        }

        [HttpGet("inattesa")]
        [Authorize(Roles = Ruoli.Admin)]
        [ProducesResponseType(typeof(List<FilmDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetInAttesa()
        {
            var film = await _filmService.GetFilmInAttesa();
            return Ok(film);
        }

        // Admin: unisce due schede dello stesso film. {id} resta, {idDaEliminare} sparisce
        // e tutto quello che lo riguardava (recensioni, preferiti, visioni, liste…) passa a {id}.
        [HttpPost("{id}/unisci/{idDaEliminare}")]
        [Authorize(Roles = Ruoli.Admin)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Unisci(int id, int idDaEliminare)
        {
            if (id == idDaEliminare) return BadRequest("Scegli due film diversi");
            var esito = await _filmService.UnisciFilm(id, idDaEliminare);
            if (esito == null) return NotFound("Film non trovato");
            if (esito == false) return BadRequest("Non si può unire un film con una serie TV");
            return NoContent();
        }
    }
}