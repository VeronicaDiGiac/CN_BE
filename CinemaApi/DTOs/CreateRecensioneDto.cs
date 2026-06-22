namespace CinemaApi.DTOs
{
    public class CreateRecensioneDto
    {
        public int IdUtente { get; set; }
        public int IdFilm { get; set; }
        public int? Voto { get; set; }
        public string? Contenuto { get; set; }
    }
}