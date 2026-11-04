namespace CinemaApi.DTOs.ResponseDto
{
    // Pannello di controllo dell'admin (GET /api/admin/...)

    // Numeri del sito: totali e quanti sono nuovi dalla data indicata (es. ultima visita del pannello)
    public class RiepilogoAdminDto
    {
        public DateTime Dal { get; set; }
        public int Utenti { get; set; }
        public int NuoviUtenti { get; set; }
        public int Recensioni { get; set; }
        public int NuoveRecensioni { get; set; }
        public int Discussioni { get; set; }
        public int NuoveDiscussioni { get; set; }
        public int Commenti { get; set; }
        public int NuoviCommenti { get; set; }
        public int FilmInAttesa { get; set; }
        public int SegnalazioniAperte { get; set; }
    }

    // Una voce dell'attività recente: iscrizione, recensione, discussione o commento
    public class AttivitaAdminDto
    {
        // Uno dei valori di Helpers.TipiAttivitaAdmin
        public string Tipo { get; set; } = "";
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        public int? IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public int? IdDiscussione { get; set; }
        public int? IdRecensione { get; set; }
        // Inizio del testo (titolo della discussione, recensione, commento)
        public string? Testo { get; set; }
    }

    // Niente email né altri dati personali: per moderare bastano nome e attività
    public class UtenteAdminDto
    {
        public int IdUtente { get; set; }
        public string UserName { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public string Ruolo { get; set; } = "";
        public DateTime? DataDiRegistrazione { get; set; }
        // Entrato con Google (nessuna password salvata)
        public bool AccessoGoogle { get; set; }
        public int NumRecensioni { get; set; }
        public int NumDiscussioni { get; set; }
        public int NumCommenti { get; set; }
        // Segnalazioni aperte sui suoi contenuti
        public int SegnalazioniAperte { get; set; }
    }

    public class RecensioneAdminDto
    {
        public int IdRecensione { get; set; }
        public int IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        public int? Voto { get; set; }
        public string? Contenuto { get; set; }
        public bool Spoiler { get; set; }
        public DateTime? Data { get; set; }
        public int NumCommenti { get; set; }
        public int NumMiPiace { get; set; }
        public int SegnalazioniAperte { get; set; }
    }

    public class DiscussioneAdminDto
    {
        public int IdDiscussione { get; set; }
        public int IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        public string Titolo { get; set; } = "";
        public string? Contenuto { get; set; }
        public int? Stagione { get; set; }
        public int? Episodio { get; set; }
        public DateTime Data { get; set; }
        public int NumMessaggi { get; set; }
        public int SegnalazioniAperte { get; set; }
    }

    public class CommentoAdminDto
    {
        public int IdCommento { get; set; }
        public int IdUtente { get; set; }
        public string? UserName { get; set; }
        public string? Contenuto { get; set; }
        public DateTime? Data { get; set; }
        public int? IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public int? IdRecensione { get; set; }
        public int? IdDiscussione { get; set; }
        public string? TitoloDiscussione { get; set; }
        public int SegnalazioniAperte { get; set; }
    }
}
