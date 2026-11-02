namespace CinemaApi.DTOs.ResponseDto
{
    // Risposta 409 quando si propone un film che c'è già: il FE può mandare l'utente alla scheda esistente
    public class FilmDoppioneDto
    {
        public string Messaggio { get; set; } = "";
        public int IdFilm { get; set; }
        public string? Titolo { get; set; }
        public int? AnnoUscita { get; set; }
        // "Approvato" (scheda visibile) oppure "InAttesa" (già proposto, in attesa di revisione)
        public string Stato { get; set; } = "";
    }
}
