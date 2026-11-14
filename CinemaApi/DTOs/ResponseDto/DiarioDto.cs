namespace CinemaApi.DTOs.ResponseDto
{
    // Una voce del diario con i dati del film già pronti per la UI
    public class VisioneDto
    {
        public int IdVisione { get; set; }
        public int IdUtente { get; set; }
        public int IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public int? AnnoUscita { get; set; }
        public string? ImmagineUrl { get; set; }
        // null se l'utente non ha indicato quando l'ha visto
        public DateOnly? DataVisione { get; set; }
        // Voto della recensione dell'utente su quel film, se l'ha scritta
        public int? Voto { get; set; }
    }

    public class DaVedereDto
    {
        public int IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public string? Regista { get; set; }
        public int? AnnoUscita { get; set; }
        public string? ImmagineUrl { get; set; }
        public DateTime DataAggiunta { get; set; }
    }

    // Stato dei film per l'utente autenticato: solo gli id, per segnare "Visto" e "Da vedere"
    // sulle copertine del catalogo con una sola richiesta
    public class StatoFilmUtenteDto
    {
        public List<int> Visti { get; set; } = new();
        public List<int> DaVedere { get; set; } = new();
    }
}
