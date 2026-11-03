namespace CinemaApi.DTOs.ResponseDto
{
    public class NotificaDto
    {
        public int IdNotifica { get; set; }
        // Uno dei valori di Helpers.TipiNotifica
        public string Tipo { get; set; } = "";
        public bool Letta { get; set; }
        public DateTime DataCreazione { get; set; }

        // null per le notifiche di sistema (nuovo episodio)
        public int? IdMittente { get; set; }
        public string? UserNameMittente { get; set; }
        public string? AvatarMittente { get; set; }

        public int? IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public int? IdRecensione { get; set; }
        public int? IdCommento { get; set; }
        // Solo per NuovoEpisodio
        public int? Stagione { get; set; }
        public int? Episodio { get; set; }
    }
}
