namespace CinemaApi.DTOs.ResponseDto
{

    public class FollowDto
    {
        public int IdFollow { get; set; }
        public int IdUtente { get; set; }       // l'utente mostrato (follower o following a seconda del contesto)
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? DataFollow { get; set; }
    }
}