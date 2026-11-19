namespace CinemaApi.DTOs.RequestDto
{
    public class RecensioneDto
    {
        public int IdRecensione { get; set; }

        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        // true se l'autore è un amministratore: il FE mostra l'etichetta "Admin" accanto al nome
        public bool AutoreAdmin { get; set; }
        public string? TitoloFilm { get; set; }
        // Locandina del film: la mostra la pagina della singola recensione
        public string? ImmagineUrlFilm { get; set; }
        // Voto dell'autore al film (da VotiFilm: si può cambiare anche senza toccare la recensione)
        public int? Voto { get; set; }
        public string? Contenuto { get; set; }
        public DateTime? DataRecensione { get; set; }
        public int IdFilm { get; set; }
        public bool Spoiler { get; set; }
        public int NumeroMiPiace { get; set; }
        // true se l'utente autenticato ha messo "mi piace" (sempre false per chi non è loggato)
        public bool MiPiaceUtente { get; set; }
        public int NumeroCommenti { get; set; }
        // Stelle date alla recensione dagli altri utenti, e quelle date dall'utente autenticato
        public double? VotoMedioRecensione { get; set; }
        public int NumeroVotiRecensione { get; set; }
        public int? MioVotoRecensione { get; set; }
    }
}