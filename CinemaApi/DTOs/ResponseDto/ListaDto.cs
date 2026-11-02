namespace CinemaApi.DTOs.ResponseDto
{
    // Lista vista "da fuori": per il profilo e per la scelta "Aggiungi a una lista"
    public class ListaRiassuntoDto
    {
        public int IdLista { get; set; }
        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        public string Titolo { get; set; } = "";
        public string? Descrizione { get; set; }
        public bool Pubblica { get; set; }
        public DateTime DataModifica { get; set; }
        public int NumeroFilm { get; set; }
        // Locandine dei primi film, per l'anteprima
        public List<string> Copertine { get; set; } = new();
        // Solo per GET /api/liste/mie?idFilm=: true se quel film è già nella lista
        public bool ContieneFilm { get; set; }
    }

    public class ListaFilmDto
    {
        public int IdFilm { get; set; }
        public int Posizione { get; set; }
        public string? Nota { get; set; }
        public string? Titolo { get; set; }
        public int? AnnoUscita { get; set; }
        public string? Regista { get; set; }
        public string? ImmagineUrl { get; set; }
    }

    public class ListaDettaglioDto
    {
        public int IdLista { get; set; }
        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        public string Titolo { get; set; } = "";
        public string? Descrizione { get; set; }
        public bool Pubblica { get; set; }
        public DateTime DataCreazione { get; set; }
        public DateTime DataModifica { get; set; }
        public List<ListaFilmDto> Film { get; set; } = new();
    }
}
