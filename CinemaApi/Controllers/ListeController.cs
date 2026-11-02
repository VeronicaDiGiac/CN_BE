using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApi.Controllers
{
    // Liste personali. Le modifica solo il proprietario; l'admin può solo eliminarle (moderazione).
    [ApiController]
    [Route("api/[controller]")]
    public class ListeController : ControllerBase
    {
        private readonly ListeService _listeService;
        public ListeController(ListeService listeService) { _listeService = listeService; }

        [HttpGet("utente/{idUtente}")]
        [ProducesResponseType(typeof(List<ListaRiassuntoDto>), 200)]
        public async Task<IActionResult> GetListeUtente(int idUtente) =>
            Ok(await _listeService.GetListeUtente(idUtente, User.IdUtenteOpzionale()));

        // Le mie liste; con idFilm indica per ognuna se contiene già quel film
        [HttpGet("mie")]
        [Authorize]
        [ProducesResponseType(typeof(List<ListaRiassuntoDto>), 200)]
        public async Task<IActionResult> GetMie(int? idFilm) =>
            Ok(await _listeService.GetMie(User.IdUtente(), idFilm));

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ListaDettaglioDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetDettaglio(int id)
        {
            var lista = await _listeService.GetDettaglio(id, User.IdUtenteOpzionale());
            return lista == null ? NotFound() : Ok(lista);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ListaDettaglioDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Crea([FromBody] SalvaListaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Titolo)) return BadRequest("Il titolo è obbligatorio");
            var lista = await _listeService.Crea(dto, User.IdUtente());
            var dettaglio = await _listeService.GetDettaglio(lista.IdLista, lista.IdUtente);
            return CreatedAtAction(nameof(GetDettaglio), new { id = lista.IdLista }, dettaglio);
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Aggiorna(int id, [FromBody] SalvaListaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Titolo)) return BadRequest("Il titolo è obbligatorio");
            var (lista, errore) = await ListaDelProprietario(id);
            if (errore != null) return errore;

            await _listeService.Aggiorna(lista!, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Elimina(int id)
        {
            var lista = await _listeService.GetLista(id);
            if (lista == null) return NotFound();
            if (!AutorizzazioneHelper.PuoModificare(lista.IdUtente, User.IdUtente(), User.Ruolo())) return Forbid();

            await _listeService.Elimina(lista);
            return NoContent();
        }

        // PUT perché è idempotente: se il film c'è già si aggiorna solo la nota
        [HttpPut("{id}/film/{idFilm}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AggiungiFilm(int id, int idFilm, [FromBody] AggiungiFilmListaDto? dto)
        {
            var (lista, errore) = await ListaDelProprietario(id);
            if (errore != null) return errore;
            if (!await _listeService.FilmEsiste(idFilm)) return NotFound("Film non trovato");

            await _listeService.AggiungiFilm(lista!, idFilm, dto?.Nota);
            return NoContent();
        }

        [HttpDelete("{id}/film/{idFilm}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RimuoviFilm(int id, int idFilm)
        {
            var (lista, errore) = await ListaDelProprietario(id);
            if (errore != null) return errore;

            return await _listeService.RimuoviFilm(lista!, idFilm) ? NoContent() : NotFound();
        }

        [HttpPut("{id}/ordine")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Riordina(int id, [FromBody] OrdineListaDto dto)
        {
            var (lista, errore) = await ListaDelProprietario(id);
            if (errore != null) return errore;

            return await _listeService.Riordina(lista!, dto.IdFilm)
                ? NoContent()
                : BadRequest("L'ordine deve contenere tutti e soli i film della lista");
        }

        // La lista se esiste ed è dell'utente autenticato, altrimenti la risposta di errore da restituire
        private async Task<(Lista? lista, IActionResult? errore)> ListaDelProprietario(int id)
        {
            var lista = await _listeService.GetLista(id);
            if (lista == null) return (null, NotFound());
            if (lista.IdUtente != User.IdUtente()) return (null, Forbid());
            return (lista, null);
        }
    }
}
