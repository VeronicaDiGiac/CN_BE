namespace CinemaApi.DTOs.ResponseDto
{
    public class CommentoDto
    {
        public int IdCommento { get; set; }
        public int? IdRecensione { get; set; }
        public int? IdDiscussione { get; set; }

        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        // true se l'autore è un amministratore: il FE mostra l'etichetta "Admin" accanto al nome
        public bool AutoreAdmin { get; set; }
        public string? Contenuto { get; set; }
        public DateTime? DataCommento { get; set; }
        public int? IdCommentoPadre { get; set; }
        public bool Spoiler { get; set; }
        public int NumeroMiPiace { get; set; }
        // true se l'utente autenticato ha messo "mi piace"
        public bool MiPiaceUtente { get; set; }
        // Tipo ricorsivo: CommentoDto contiene una lista di CommentoDto
        public List<CommentoDto>? Risposte { get; set; }
    }
}