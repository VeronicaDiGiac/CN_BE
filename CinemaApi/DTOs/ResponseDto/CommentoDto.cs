namespace CinemaApi.DTOs.ResponseDto
{
    public class CommentoDto
    {
        public int IdCommento { get; set; }

        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        public string? Contenuto { get; set; }
        public DateTime? DataCommento { get; set; }
        public int? IdCommentoPadre { get; set; }
        // Tipo ricorsivo: CommentoDto contiene una lista di CommentoDto
        public List<CommentoDto>? Risposte { get; set; }
    }
}