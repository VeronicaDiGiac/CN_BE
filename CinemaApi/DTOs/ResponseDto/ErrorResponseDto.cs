namespace CinemaApi.DTOs.ResponseDto
{
    public class ErrorResponseDto
    {
        public int StatusCode { get; set; }
        public string Messaggio { get; set; } = "";   // testo pronto per l'utente
        public string? Dettaglio { get; set; }        // extra opzionale (debug/log)
    }
}