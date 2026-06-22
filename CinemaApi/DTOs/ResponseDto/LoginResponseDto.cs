namespace CinemaApi.DTOs.ResponseDto
{
    public class LoginResponseDto
    {
        public int IdUtente { get; set; }
        public string UserName { get; set; }
        public string Ruolo { get; set; }
        public string Token { get; set; } 
    }
}
