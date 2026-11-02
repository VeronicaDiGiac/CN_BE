namespace CinemaApi.DTOs.ResponseDto
{
    public class PreferitoDto
    {
        public int IdPreferito { get; set; }
        public int IdUtente { get; set; }
        public int IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public string? Regista { get; set; }
        public DateTime? DataPreferito { get; set; }
    }
}
