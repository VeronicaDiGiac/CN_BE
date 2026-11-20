namespace CinemaApi.DTOs.ResponseDto
{
    // Chi sta guardando cosa, in questo momento ("Sto guardando"): per la scheda, il profilo e Attività
    public class StaGuardandoDto
    {
        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }
        public int IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public string? ImmagineUrl { get; set; }
        // "Film" o "Serie"
        public string Tipo { get; set; } = "";
        public DateTime Inizio { get; set; }
        // Dopo quest'ora lo stato sparisce da solo
        public DateTime Fine { get; set; }
    }

    // Stato del follow verso una persona dal suo profilo: la segui? hai la campanella accesa?
    public class AvvisiFollowDto
    {
        public bool Segui { get; set; }
        public bool Avvisi { get; set; }
    }
}
