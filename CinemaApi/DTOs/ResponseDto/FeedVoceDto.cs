namespace CinemaApi.DTOs.ResponseDto
{
    // Una voce del feed: "X ha visto Y" oppure "X ha recensito Y"
    public class FeedVoceDto
    {
        // Uno dei valori di Helpers.TipiAttivita
        public string Tipo { get; set; } = "";
        public DateTime Data { get; set; }

        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }

        public int IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public string? ImmagineUrl { get; set; }

        // Solo per le recensioni
        public int? IdRecensione { get; set; }
        public int? Voto { get; set; }
        public string? Testo { get; set; }
        public bool Spoiler { get; set; }
    }
}
