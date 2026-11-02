namespace CinemaApi.DTOs.ResponseDto
{
    // Un risultato della ricerca su TMDB
    public class TmdbRisultatoDto
    {
        public int TmdbId { get; set; }
        public string Titolo { get; set; } = "";
        public string? TitoloOriginale { get; set; }
        public int? AnnoUscita { get; set; }
        public string? LocandinaUrl { get; set; }
        public string? Descrizione { get; set; }

        // Valorizzato se il film è già nel catalogo: il FE apre direttamente la scheda
        public int? IdFilm { get; set; }
    }

    public class TmdbRicercaDto
    {
        public int Pagina { get; set; }
        public int TotalePagine { get; set; }
        public int TotaleRisultati { get; set; }
        public List<TmdbRisultatoDto> Risultati { get; set; } = new();
    }

    // Risposta dell'importazione: id del film nel catalogo
    public class TmdbImportazioneDto
    {
        public int IdFilm { get; set; }
        // false se il film era già nel catalogo
        public bool Creato { get; set; }
    }

    // Proposta di abbinamento tra un film già nel catalogo e un film TMDB
    public class TmdbAbbinamentoDto
    {
        public int IdFilm { get; set; }
        public string? Titolo { get; set; }
        public int? AnnoUscita { get; set; }
        // null se su TMDB non si è trovato niente
        public TmdbRisultatoDto? Proposta { get; set; }
    }
}
