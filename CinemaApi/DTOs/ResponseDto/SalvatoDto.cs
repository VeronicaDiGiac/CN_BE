namespace CinemaApi.DTOs.ResponseDto
{
    // Una recensione o una discussione salvata, con quello che serve per mostrarla in elenco
    // (GET /api/salvati)
    public class SalvatoDto
    {
        // "Recensione" o "Discussione"
        public string Tipo { get; set; } = "";
        public int IdContenuto { get; set; }
        public DateTime DataSalvataggio { get; set; }

        public int IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public string? ImmagineFilm { get; set; }

        public int IdAutore { get; set; }
        public string? Autore { get; set; }

        // Titolo della discussione (null per le recensioni)
        public string? Titolo { get; set; }
        public string? Testo { get; set; }
        public bool Spoiler { get; set; }
        // Solo recensioni
        public int? Voto { get; set; }
        // Solo discussioni di un episodio
        public int? Stagione { get; set; }
        public int? Episodio { get; set; }

        public int NumeroMiPiace { get; set; }
        // Commenti della recensione o messaggi della discussione
        public int NumeroCommenti { get; set; }
    }

    // Cosa ha salvato l'utente, per accendere i segnalibri nel FE (GET /api/salvati/chiavi)
    public class ChiaveSalvatoDto
    {
        public string Tipo { get; set; } = "";
        public int IdContenuto { get; set; }
    }
}
