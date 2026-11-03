namespace CinemaApi.DTOs.ResponseDto
{
    // Un risultato della ricerca su TMDB
    public class TmdbRisultatoDto
    {
        public int TmdbId { get; set; }
        // "Film" o "Serie": su TMDB hanno numerazioni separate, l'id da solo non basta
        public string Tipo { get; set; } = "";
        public string Titolo { get; set; } = "";
        public string? TitoloOriginale { get; set; }
        // Per le serie: anno della prima puntata
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
        // Minuti secondo TMDB (usata anche per completare Film.DurataMinuti)
        public int? DurataMinuti { get; set; }
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

    // Una stagione di una serie (elenco nella scheda)
    public class StagioneDto
    {
        // 0 = episodi speciali
        public int Numero { get; set; }
        public string Nome { get; set; } = "";
        public int NumeroEpisodi { get; set; }
        public string? DataUscita { get; set; }
        public string? LocandinaUrl { get; set; }
    }

    // Una stagione con i suoi episodi
    public class StagioneDettaglioDto
    {
        public int Numero { get; set; }
        public string Nome { get; set; } = "";
        public string? Descrizione { get; set; }
        public List<EpisodioDto> Episodi { get; set; } = new();
    }

    public class EpisodioDto
    {
        public int Numero { get; set; }
        public string Titolo { get; set; } = "";
        // 'YYYY-MM-DD'; null se non è ancora stata annunciata
        public string? DataUscita { get; set; }
        public int? DurataMinuti { get; set; }
        public string? Descrizione { get; set; }
        public string? ImmagineUrl { get; set; }
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
