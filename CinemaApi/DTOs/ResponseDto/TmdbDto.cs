namespace CinemaApi.DTOs.ResponseDto
{
    // Un risultato della ricerca su TMDB
    public class TmdbRisultatoDto
    {
        public int TmdbId { get; set; }
        public string Titolo { get; set; } = "";
        public string? TitoloOriginale { get; set; }
        public int? AnnoUscita { get; set; }
        // Data completa 'YYYY-MM-DD' (serve per le uscite in arrivo)
        public string? DataUscita { get; set; }
        public string? LocandinaUrl { get; set; }
        public string? Descrizione { get; set; }

        // Valorizzato se il film è già nel catalogo: il FE apre direttamente la scheda
        public int? IdFilm { get; set; }
    }

    // Trailer e "dove guardarlo" in Italia. Tutto vuoto se il film non è collegato a TMDB.
    public class FilmExtraDto
    {
        // Chiave del video YouTube (il FE costruisce l'URL di incorporamento)
        public string? TrailerYoutube { get; set; }
        // Pagina TMDB/JustWatch con tutte le offerte
        public string? LinkDoveGuardarlo { get; set; }
        public List<PiattaformaDto> Abbonamento { get; set; } = new();
        public List<PiattaformaDto> Noleggio { get; set; } = new();
        public List<PiattaformaDto> Acquisto { get; set; } = new();
    }

    public class PiattaformaDto
    {
        public string Nome { get; set; } = "";
        public string? LogoUrl { get; set; }
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
