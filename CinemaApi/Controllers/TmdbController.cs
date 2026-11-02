using CinemaApi.Helpers;
using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CinemaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting(LimiteTmdb)]
    public class TmdbController : ControllerBase
    {
        // Nome della policy di rate limiting registrata in Program.cs:
        // protegge la quota TMDB e il catalogo da importazioni a raffica
        public const string LimiteTmdb = "tmdb";

        private readonly TmdbService _tmdbService;
        private readonly FilmService _filmService;

        public TmdbController(TmdbService tmdbService, FilmService filmService)
        {
            _tmdbService = tmdbService;
            _filmService = filmService;
        }

        // Ricerca pubblica su TMDB. Per i film già nel catalogo c'è anche IdFilm.
        [HttpGet("cerca")]
        [ProducesResponseType(typeof(TmdbRicercaDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(429)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Cerca(string q, int pagina = 1)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return BadRequest("Scrivi almeno 2 caratteri");
            if (pagina < 1 || pagina > 500) return BadRequest("Pagina non valida");

            var risultato = await _tmdbService.Cerca(q.Trim(), pagina);
            await SegnaGiaNelCatalogo(risultato);
            return Ok(risultato);
        }

        // Prossime uscite nei cinema italiani (pubblico)
        [HttpGet("uscite")]
        [ProducesResponseType(typeof(TmdbRicercaDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(429)]
        public async Task<IActionResult> Uscite(int pagina = 1)
        {
            if (pagina < 1 || pagina > 500) return BadRequest("Pagina non valida");
            var risultato = await _tmdbService.UsciteInArrivo(pagina);
            await SegnaGiaNelCatalogo(risultato);
            return Ok(risultato);
        }

        // Trailer e "dove guardarlo" di un film del catalogo (vuoto se non è collegato a TMDB)
        [HttpGet("film/{idFilm}/extra")]
        [ProducesResponseType(typeof(FilmExtraDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(429)]
        public async Task<IActionResult> Extra(int idFilm)
        {
            var film = await _filmService.GetFilmById(idFilm);
            if (film == null || film.Stato != StatiFilm.Approvato) return NotFound();
            if (film.TmdbId == null) return Ok(new FilmExtraDto());
            return Ok(await _tmdbService.Extra(film.TmdbId.Value));
        }

        // Valorizza IdFilm sui film TMDB che sono già nel catalogo
        private async Task SegnaGiaNelCatalogo(TmdbRicercaDto risultato)
        {
            var giaPresenti = await _filmService.IdFilmPerTmdb(risultato.Risultati.Select(r => r.TmdbId));
            foreach (var r in risultato.Risultati)
                r.IdFilm = giaPresenti.TryGetValue(r.TmdbId, out var idFilm) ? idFilm : null;
        }

        // Porta un film TMDB nel catalogo (se non c'è già) e restituisce il suo IdFilm.
        // Pubblico: anche chi non è registrato deve poter aprire la scheda di un film.
        [HttpPost("importa/{tmdbId}")]
        [ProducesResponseType(typeof(TmdbImportazioneDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(429)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Importa(int tmdbId)
        {
            var dati = await _tmdbService.Dettaglio(tmdbId);
            if (dati == null) return NotFound("Film non trovato su TMDB");

            var risultato = await _filmService.ImportaDaTmdb(dati);
            return Ok(risultato);
        }

        // Admin: per ogni film del catalogo non ancora collegato propone il film TMDB
        // più probabile (stesso titolo e anno). Non modifica niente.
        [HttpGet("abbinamenti")]
        [Authorize(Roles = Ruoli.Admin)]
        [ProducesResponseType(typeof(List<TmdbAbbinamentoDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Abbinamenti()
        {
            var proposte = new List<TmdbAbbinamentoDto>();
            foreach (var film in await _filmService.GetFilmSenzaTmdb())
            {
                TmdbRisultatoDto? proposta = null;
                if (!string.IsNullOrWhiteSpace(film.Titolo))
                {
                    var ricerca = await _tmdbService.Cerca(film.Titolo, 1, film.AnnoUscita);
                    // Se con l'anno non si trova niente si riprova senza
                    if (ricerca.Risultati.Count == 0 && film.AnnoUscita != null)
                        ricerca = await _tmdbService.Cerca(film.Titolo);
                    proposta = ricerca.Risultati.FirstOrDefault();
                }

                proposte.Add(new TmdbAbbinamentoDto
                {
                    IdFilm = film.IdFilm,
                    Titolo = film.Titolo,
                    AnnoUscita = film.AnnoUscita,
                    Proposta = proposta
                });
            }
            return Ok(proposte);
        }

        // Admin: applica gli abbinamenti confermati. Riempie solo i campi vuoti del film.
        [HttpPost("collega")]
        [Authorize(Roles = Ruoli.Admin)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Collega([FromBody] List<CollegaTmdbDto> abbinamenti)
        {
            if (abbinamenti.Select(a => a.TmdbId).Distinct().Count() != abbinamenti.Count)
                return BadRequest("Lo stesso film TMDB è abbinato a più film");

            foreach (var a in abbinamenti)
            {
                var dati = await _tmdbService.Dettaglio(a.TmdbId);
                if (dati == null) return NotFound($"Film TMDB {a.TmdbId} non trovato");

                var esito = await _filmService.CollegaTmdb(a.IdFilm, dati);
                if (esito == null) return NotFound($"Film {a.IdFilm} non trovato");
                if (esito == false) return Conflict($"Il film TMDB {a.TmdbId} è già collegato a un altro film");
            }
            return NoContent();
        }
    }
}
