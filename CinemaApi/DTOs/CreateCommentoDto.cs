namespace CinemaApi.DTOs
{
    public class CreateCommentoDto
    {
        public int IdUtente { get; set; }
        public int IdRecensione { get; set; }
        public string? Contenuto { get; set; }
        public int? IdCommentoPadre { get; set; }
    }
}