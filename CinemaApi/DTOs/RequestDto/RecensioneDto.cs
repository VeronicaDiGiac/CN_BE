namespace CinemaApi.DTOs.RequestDto
{
    public class RecensioneDto
    {
        public int IdRecensione { get; set; }

        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        public string? TitoloFilm { get; set; }
        public int? Voto { get; set; }
        public string? Contenuto { get; set; }
        public DateTime? DataRecensione { get; set; }
        public int IdFilm { get; set; }
        public bool Spoiler { get; set; }
        public int NumeroMiPiace { get; set; }
        // true se l'utente autenticato ha messo "mi piace" (sempre false per chi non è loggato)
        public bool MiPiaceUtente { get; set; }
    }
}