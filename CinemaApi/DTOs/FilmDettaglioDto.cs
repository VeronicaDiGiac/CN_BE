namespace CinemaApi.DTOs
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
    }
}