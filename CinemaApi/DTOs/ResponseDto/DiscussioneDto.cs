namespace CinemaApi.DTOs.ResponseDto
{
    public class DiscussioneDto
    {
        public int IdDiscussione { get; set; }
        public int IdFilm { get; set; }
        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        // true se l'autore è un amministratore: il FE mostra l'etichetta "Admin" accanto al nome
        public bool AutoreAdmin { get; set; }
        public string? AvatarUrl { get; set; }
        public string Titolo { get; set; } = "";
        public string? Contenuto { get; set; }
        public bool Spoiler { get; set; }
        // Discussione su un episodio (solo serie)
        public int? Stagione { get; set; }
        public int? Episodio { get; set; }
        // true se riguarda un episodio che chi guarda non ha ancora visto (o chi guarda non è loggato):
        // il FE la nasconde come uno spoiler
        public bool EpisodioNonVisto { get; set; }
        public DateTime DataCreazione { get; set; }
        public DateTime DataUltimaAttivita { get; set; }
        public int NumeroCommenti { get; set; }
        public int NumeroMiPiace { get; set; }
        public bool MiPiaceUtente { get; set; }
    }
}
