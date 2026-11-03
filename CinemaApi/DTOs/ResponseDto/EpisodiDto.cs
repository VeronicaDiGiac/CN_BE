namespace CinemaApi.DTOs.ResponseDto
{
    // Avanzamento di un utente in una serie (scheda della serie)
    public class AvanzamentoSerieDto
    {
        public int IdFilm { get; set; }
        // Episodi visti e usciti, senza gli speciali (stagione 0)
        public int EpisodiVisti { get; set; }
        public int EpisodiUsciti { get; set; }
        // Helpers.StatiSerie: NonIniziata, InCorso, InPari, Completata
        public string Stato { get; set; } = "";
        // Tutti gli episodi segnati, speciali compresi
        public List<EpisodioVistoDto> Visti { get; set; } = new();
        // Primo episodio già uscito che l'utente non ha ancora visto
        public EpisodioRiferimentoDto? Prossimo { get; set; }
        // Prossimo episodio annunciato e non ancora uscito
        public EpisodioRiferimentoDto? InUscita { get; set; }
    }

    public class EpisodioVistoDto
    {
        public int Stagione { get; set; }
        public int Episodio { get; set; }
        public DateOnly DataVisione { get; set; }
    }

    public class EpisodioRiferimentoDto
    {
        public int Stagione { get; set; }
        public int Episodio { get; set; }
        public string? Titolo { get; set; }
        // 'YYYY-MM-DD'
        public string? DataUscita { get; set; }
    }

    // Una puntata nel calendario delle serie seguite
    public class PuntataCalendarioDto
    {
        public int IdFilm { get; set; }
        public string? Titolo { get; set; }
        public string? ImmagineUrl { get; set; }
        public int Stagione { get; set; }
        public int Episodio { get; set; }
        public string? TitoloEpisodio { get; set; }
        // 'YYYY-MM-DD'
        public string DataUscita { get; set; } = "";
        // L'utente l'ha già segnata come vista
        public bool Visto { get; set; }
    }

    // Una serie seguita da un utente (profilo: "Serie in corso", "In pari", "Completate")
    public class SerieUtenteDto
    {
        public int IdFilm { get; set; }
        public string? Titolo { get; set; }
        public string? ImmagineUrl { get; set; }
        public int EpisodiVisti { get; set; }
        public int EpisodiUsciti { get; set; }
        public string Stato { get; set; } = "";
        public int? ProssimaStagione { get; set; }
        public int? ProssimoEpisodio { get; set; }
        public DateOnly UltimaVisione { get; set; }
    }
}
