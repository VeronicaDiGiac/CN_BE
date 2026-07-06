namespace CinemaApi.DTOs.ResponseDto
{
    // DTO di risposta mostrato all'Admin quando visualizza le segnalazioni.
    // Include lo UserName di chi ha segnalato, per contesto.
    public class SegnalazioneDto
    {
        public int IdSegnalazione { get; set; }
        public int IdUtente { get; set; }
        public string? UserName { get; set; }         // chi ha segnalato
        public string TipoContenuto { get; set; } = "";
        public int IdContenuto { get; set; }
        public string? Motivo { get; set; }
        public DateTime? DataSegnalazione { get; set; }
    }
}