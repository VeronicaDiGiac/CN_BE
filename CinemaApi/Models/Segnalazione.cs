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

        //Nuove prop 
        public string Stato { get; set; } = "Aperta";
        public DateTime? DataGestione { get; set; }
        
        //Non so se mi serve la foreignKey effettivamente o solo leggere il numero dell'amministratore e non pure il nome ecc
        public int? IdAdminGestore { get; set; }
    }
}