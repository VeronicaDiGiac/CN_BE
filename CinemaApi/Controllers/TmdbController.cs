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

        // Ricerca pubblica su TMDB di film (predefinito) o serie (tipo=Serie).
        // Per i titoli già nel catalogo c'è anche IdFilm.
        [HttpGet("cerca")]
        [ProducesResponseType(typeof(TmdbRicercaDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(429)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Cerca(string q, int pagina = 1, string? tipo = null)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return BadRequest("Scrivi almeno 2 caratteri");
            if (pagina < 1 || pagina > 500) return BadRequest("Pagina non valida");
            var tipoTitolo = TipiTitolo.Normalizza(tipo ?? TipiTitolo.Film);
            if (tipoTitolo == null) return BadRequest("Tipo non valido: Film o Serie");

            var risultato = await _tmdbService.Cerca(q.Trim(), pagina, tipo: tipoTitolo);
            await SegnaGiaNelCatalogo(risultato);
            return Ok(risultato);
        }

        // Attori e registi che corrispondono al testo, con i loro titoli più noti
        // (la barra di ricerca: "leonardo di caprio" deve trovare i suoi film, non solo i titoli che lo citano)
        [HttpGet("persone")]
        [ProducesResponseType(typeof(List<PersonaTmdbDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(429)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Persone(string q, string? tipo = null)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
                return BadRequest("Scrivi almeno 3 caratteri");
            var tipoTitolo = tipo == null ? null : TipiTitolo.Normalizza(tipo);
            if (tipo != null && tipoTitolo == null) return BadRequest("Tipo non valido: Film o Serie");

            var persone = await _tmdbService.CercaPersone(q.Trim(), tipoTitolo);
            foreach (var p in persone)
                await SegnaGiaNelCatalogo(new TmdbRicercaDto { Risultati = p.Titoli });
            return Ok(persone);
        }

        // Elenchi pubblici per il catalogo: alcinema, tendenze, popolari, inarrivo,
        // serie-tendenze, serie-popolari, serie-inonda (Helpers.CategorieTmdb)
        [HttpGet("elenco/{categoria}")]
        [ProducesResponseType(typeof(TmdbRicercaDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(429)]
        public async Task<IActionResult> Elenco(string categoria, int pagina = 1)
        {
            if (pagina < 1 || pagina > 500) return BadRequest("Pagina non valida");
            var richiesta = _tmdbService.Elenco(categoria.ToLowerInvariant(), pagina);
            if (richiesta == null) return NotFound("Categoria sconosciuta");

            var risultato = await richiesta;
            await SegnaGiaNelCatalogo(risultato);
            return Ok(risultato);
        }

        // Trailer e "dove guardarlo" di un film o di una serie del catalogo (vuoto se non è collegato a TMDB)
        [HttpGet("film/{idFilm}/extra")]
        [ProducesResponseType(typeof(FilmExtraDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(429)]
        public async Task<IActionResult> Extra(int idFilm)
        {
            var film = await _filmService.GetFilmById(idFilm);
            if (film == null || film.Stato != StatiFilm.Approvato) return NotFound();
            if (film.TmdbId == null) return Ok(new FilmExtraDto());

            var extra = await _tmdbService.Extra(film.TmdbId.Value, film.Tipo);
            // I film collegati prima che esistesse la durata la ricevono alla prima apertura della scheda
            if (film.DurataMinuti == null && extra.DurataMinuti != null)
                await _filmService.ImpostaDurata(film, extra.DurataMinuti.Value);
            return Ok(extra);
        }

        // Stagioni di una serie del catalogo
        [HttpGet("serie/{idFilm}/stagioni")]
        [ProducesResponseType(typeof(List<StagioneDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(429)]
        public async Task<IActionResult> Stagioni(int idFilm)
        {
            var tmdbId = await TmdbIdSerie(idFilm);
            if (tmdbId == null) return NotFound("Serie non trovata");
            var stagioni = await _tmdbService.Stagioni(tmdbId.Value);
            return stagioni == null ? NotFound("Serie non trovata su TMDB") : Ok(stagioni);
        }

        // Episodi di una stagione di una serie del catalogo
        [HttpGet("serie/{idFilm}/stagioni/{numero}")]
        [ProducesResponseType(typeof(StagioneDettaglioDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(429)]
        public async Task<IActionResult> Stagione(int idFilm, int numero)
        {
            if (numero < 0) return NotFound("Stagione non trovata");
            var tmdbId = await TmdbIdSerie(idFilm);
            if (tmdbId == null) return NotFound("Serie non trovata");
            var stagione = await _tmdbService.Stagione(tmdbId.Value, numero);
            return stagione == null ? NotFound("Stagione non trovata") : Ok(stagione);
        }

        // Id TMDB di una serie visibile nel catalogo; null se non è una serie o non è collegata
        private async Task<int?> TmdbIdSerie(int idFilm)
        {
            var film = await _filmService.GetFilmById(idFilm);
            return film != null && film.Stato == StatiFilm.Approvato && film.Tipo == TipiTitolo.Serie ? film.TmdbId : null;
        }

        // Valorizza IdFilm sui titoli TMDB che sono già nel catalogo (film e serie hanno id separati)
        private async Task SegnaGiaNelCatalogo(TmdbRicercaDto risultato)
        {
            foreach (var gruppo in risultato.Risultati.GroupBy(r => r.Tipo))
            {
                var giaPresenti = await _filmService.IdFilmPerTmdb(gruppo.Select(r => r.TmdbId), gruppo.Key);
                foreach (var r in gruppo)
                    r.IdFilm = giaPresenti.TryGetValue(r.TmdbId, out var idFilm) ? idFilm : null;
            }
        }

        // Porta un film (o una serie, con tipo=Serie) TMDB nel catalogo se non c'è già
        // e restituisce il suo IdFilm. Pubblico: anche chi non è registrato deve poter aprire la scheda.
        [HttpPost("importa/{tmdbId}")]
        [ProducesResponseType(typeof(TmdbImportazioneDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(429)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Importa(int tmdbId, string? tipo = null)
        {
            var tipoTitolo = TipiTitolo.Normalizza(tipo ?? TipiTitolo.Film);
            if (tipoTitolo == null) return BadRequest("Tipo non valido: Film o Serie");

            var dati = tipoTitolo == TipiTitolo.Serie
                ? await _tmdbService.DettaglioSerie(tmdbId)
                : await _tmdbService.Dettaglio(tmdbId);
            if (dati == null) return NotFound(tipoTitolo == TipiTitolo.Serie ? "Serie non trovata su TMDB" : "Film non trovato su TMDB");

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
