using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    public class Segnalazione
    {
        [Key]
        public int IdSegnalazione { get; set; }
        public int IdUtente { get; set; }              // chi segnala
        public string TipoContenuto { get; set; } = ""; // "Recensione" o "Commento"
        public int IdContenuto { get; set; }           // id del contenuto segnalato
        public string? Motivo { get; set; }
        public DateTime? DataSegnalazione { get; set; }

        // Navigation property verso l'utente che ha fatto la segnalazione.
        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }
    }
}