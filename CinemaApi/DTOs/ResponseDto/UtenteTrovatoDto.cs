namespace CinemaApi.DTOs.ResponseDto
{
    // Un risultato della ricerca persone (GET api/utenti/cerca): solo dati pubblici, mai l'email
    public class UtenteTrovatoDto
    {
        public int IdUtente { get; set; }
        public string UserName { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public int NumeroRecensioni { get; set; }
        public int NumeroFollower { get; set; }
        // true se l'utente autenticato lo segue già (sempre false per chi non è loggato)
        public bool Seguito { get; set; }
    }
}
