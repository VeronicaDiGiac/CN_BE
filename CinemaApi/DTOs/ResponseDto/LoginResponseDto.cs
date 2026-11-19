namespace CinemaApi.DTOs.ResponseDto
{
    public class LoginResponseDto
    {
        public int IdUtente { get; set; }
        public string UserName { get; set; }
        public string Ruolo { get; set; }
        public string Token { get; set; }
        // Dura 30 giorni e vale una volta: con POST api/auth/rinnova dà una sessione nuova (e un altro token di rinnovo)
        public string TokenRinnovo { get; set; } = "";
    }
}
