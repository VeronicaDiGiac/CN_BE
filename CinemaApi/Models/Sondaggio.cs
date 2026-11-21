using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // "Sondaggio della settimana" della home (Database/HomeInterattiva.sql). Li crea l'admin; quello in
    // corso è l'ultimo con DataInizio già passata. Si vota una risposta sola, e si può cambiare.
    [Table("Sondaggi")]
    public class Sondaggio
    {
        [Key]
        public int IdSondaggio { get; set; }
        public required string Domanda { get; set; }
        public DateTime DataInizio { get; set; }
        public DateTime DataCreazione { get; set; }

        public ICollection<OpzioneSondaggio>? Opzioni { get; set; }
    }

    // Una risposta possibile, facoltativamente legata a un titolo del catalogo (si mostra la locandina)
    [Table("OpzioniSondaggio")]
    public class OpzioneSondaggio
    {
        [Key]
        public int IdOpzione { get; set; }
        public int IdSondaggio { get; set; }
        public required string Testo { get; set; }
        public int? IdFilm { get; set; }
        public int Ordine { get; set; }

        [ForeignKey("IdSondaggio")]
        public Sondaggio? Sondaggio { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }
    }

    // Voto di un utente: uno per sondaggio, chiave (IdUtente, IdSondaggio) in CinemaContext
    [Table("VotiSondaggio")]
    public class VotoSondaggio
    {
        public int IdUtente { get; set; }
        public int IdSondaggio { get; set; }
        public int IdOpzione { get; set; }
        public DateTime DataVoto { get; set; }

        [ForeignKey("IdOpzione")]
        public OpzioneSondaggio? Opzione { get; set; }
    }

    // "Film club della settimana": un titolo per settimana (DataInizio = lunedì), con la sua discussione.
    // Se l'admin non l'ha scelto, il BE lo sceglie da solo al primo accesso della settimana.
    [Table("FilmClub")]
    public class FilmClub
    {
        [Key]
        public int IdFilmClub { get; set; }
        public int IdFilm { get; set; }
        public DateOnly DataInizio { get; set; }
        // Discussione dedicata; null se non è stato possibile crearla (o se è stata eliminata)
        public int? IdDiscussione { get; set; }
        // true se l'ha scelto l'admin, false se scelto in automatico
        public bool SceltoDaAdmin { get; set; }
        public DateTime DataCreazione { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }
    }
}
