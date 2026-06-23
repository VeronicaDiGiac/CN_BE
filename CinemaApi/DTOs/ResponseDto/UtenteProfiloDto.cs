namespace CinemaApi.DTOs.ResponseDto
{
    public class UtenteProfiloDto
    {
        public int IdUtente {  get; set; }
        public string? UserName { get; set; }

        public string? Bio {  get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? DataDiRegistrazione { get; set; }

    }
}
