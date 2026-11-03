namespace CinemaApi.DTOs.ResponseDto
{
    public class FilmDettaglioDto
    {
        public int IdFilm { get; set; }
        public string? Titolo { get; set; }
        public string? Regista { get; set; }
        public int? AnnoUscita { get; set; }
        public string? Genere { get; set; }
        public string? Attori { get; set; }
        public string? ImmagineUrl { get; set; }
        public string? Descrizione { get; set; }
        public double? VotoMedio { get; set; }
        public int NumeroRecensioni { get; set; }
        public int? TmdbId { get; set; }
        // Per le serie: durata tipica di un episodio
        public int? DurataMinuti { get; set; }
        // "Film" o "Serie"
        public string Tipo { get; set; } = "";
        public int? NumeroStagioni { get; set; }

        // true se l'utente autenticato che chiama questo endpoint ha già
        // recensito questo film. Resta sempre false per chi non è loggato.
        public bool GiaRecensito { get; set; }

        // Quanti utenti diversi hanno segnato il film come visto
        public int NumeroSpettatori { get; set; }

        // Stato personale dell'utente autenticato (false/0/null per chi non è loggato)
        public bool InDaVedere { get; set; }
        public int NumeroVisioni { get; set; }
        public DateOnly? UltimaVisione { get; set; }
        // In quante liste dell'utente c'è il film
        public int NumeroListe { get; set; }
    }
}