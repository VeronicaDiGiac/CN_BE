namespace CinemaApi.DTOs
{
    public class CreateFilmDto
    {
        public int IdFilm { get; set; }

        public string? Titolo { get; set; }
        public int? AnnoUscita { get; set; }
        public string? Regista { get; set; }
        public string? Genere { get; set; }
        public string? Attori { get; set; }

        public string? ImmagineUrl { get; set; }

        public string? Descrizione { get; set; }
    }
}
