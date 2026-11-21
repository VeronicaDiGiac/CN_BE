namespace CinemaApi.DTOs.ResponseDto
{
    // Sezioni della home (HomeController, SondaggiController, FilmClubController)

    // "Continua a guardare": una serie iniziata e il prossimo episodio da vedere
    public class ContinuaGuardareDto
    {
        public int IdFilm { get; set; }
        public string? Titolo { get; set; }
        public string? ImmagineUrl { get; set; }
        public int Stagione { get; set; }
        public int Episodio { get; set; }
        public string? TitoloEpisodio { get; set; }
        public string? ImmagineEpisodio { get; set; }
        public int EpisodiVisti { get; set; }
        public int EpisodiUsciti { get; set; }
    }

    // Un titolo della lista "Da vedere" che si può vedere in abbonamento in Italia
    public class DaVedereInStreamingDto
    {
        public int IdFilm { get; set; }
        public string? Titolo { get; set; }
        public string? ImmagineUrl { get; set; }
        public string Tipo { get; set; } = "Film";
        public List<PiattaformaDto> Piattaforme { get; set; } = new();
    }

    // Classifica di Cineverse negli ultimi giorni: i più visti e i meglio votati dagli utenti
    public class ClassificaDto
    {
        public int Giorni { get; set; }
        public List<TitoloClassificaDto> PiuVisti { get; set; } = new();
        public List<TitoloClassificaDto> MeglioVotati { get; set; } = new();
    }

    public class TitoloClassificaDto
    {
        public int IdFilm { get; set; }
        public string? Titolo { get; set; }
        public string? ImmagineUrl { get; set; }
        public int? AnnoUscita { get; set; }
        public string Tipo { get; set; } = "Film";
        // Utenti che l'hanno visto nel periodo (per le serie: almeno un episodio)
        public int Spettatori { get; set; }
        // Voti dati nel periodo
        public double? VotoMedio { get; set; }
        public int NumeroVoti { get; set; }
    }

    // "Curiosità del giorno": una sezione della pagina Wikipedia di un titolo del catalogo, uguale per tutti
    public class CuriositaGiornoDto
    {
        public int IdFilm { get; set; }
        public string? Titolo { get; set; }
        public string? ImmagineUrl { get; set; }
        public int? AnnoUscita { get; set; }
        public string Lingua { get; set; } = "";
        public string TitoloSezione { get; set; } = "";
        public string Testo { get; set; } = "";
        public string Url { get; set; } = "";
        public string TitoloPagina { get; set; } = "";
    }

    // "Discussioni più animate": le discussioni con più messaggi negli ultimi giorni
    public class DiscussioneAnimataDto
    {
        public int IdDiscussione { get; set; }
        public int IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public string? ImmagineUrl { get; set; }
        public string Titolo { get; set; } = "";
        public bool Spoiler { get; set; }
        public int MessaggiRecenti { get; set; }
        public int MessaggiTotali { get; set; }
        public DateTime DataUltimaAttivita { get; set; }
        public UltimoMessaggioDto? UltimoMessaggio { get; set; }
    }

    public class UltimoMessaggioDto
    {
        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        // Tagliato; null se il messaggio è segnato come spoiler (non si mostra in home)
        public string? Anteprima { get; set; }
        public bool Spoiler { get; set; }
        public DateTime? Data { get; set; }
    }

    public class SondaggioDto
    {
        public int IdSondaggio { get; set; }
        public string Domanda { get; set; } = "";
        public DateTime DataInizio { get; set; }
        public int TotaleVoti { get; set; }
        // Risposta scelta dall'utente collegato; null se non ha votato (o non è collegato)
        public int? MiaOpzione { get; set; }
        public List<OpzioneSondaggioDto> Opzioni { get; set; } = new();
    }

    public class OpzioneSondaggioDto
    {
        public int IdOpzione { get; set; }
        public string Testo { get; set; } = "";
        public int? IdFilm { get; set; }
        public string? ImmagineUrl { get; set; }
        public int Voti { get; set; }
    }

    public class FilmClubDto
    {
        public int IdFilm { get; set; }
        public string? Titolo { get; set; }
        public string? ImmagineUrl { get; set; }
        public int? AnnoUscita { get; set; }
        public string? Regista { get; set; }
        public string? Descrizione { get; set; }
        public string Tipo { get; set; } = "Film";
        public DateOnly DataInizio { get; set; }
        public DateOnly DataFine { get; set; }
        public int? IdDiscussione { get; set; }
        public int Messaggi { get; set; }
        // Utenti che l'hanno segnato visto durante la settimana del club
        public int VistoDa { get; set; }
        public double? VotoMedio { get; set; }
        public int NumeroVoti { get; set; }
        // Per l'utente collegato
        public bool Visto { get; set; }
        public bool SceltoDaAdmin { get; set; }
    }

    // Film club della settimana in corso e risultato di quella passata
    public class FilmClubHomeDto
    {
        public FilmClubDto? Corrente { get; set; }
        public FilmClubDto? Precedente { get; set; }
    }

    // Elenco per il pannello admin
    public class SondaggioAdminDto
    {
        public int IdSondaggio { get; set; }
        public string Domanda { get; set; } = "";
        public DateTime DataInizio { get; set; }
        public int TotaleVoti { get; set; }
        // "InCorso", "Programmato" o "Concluso"
        public string Stato { get; set; } = "";
        public List<OpzioneSondaggioDto> Opzioni { get; set; } = new();
    }
}
