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
    }
}