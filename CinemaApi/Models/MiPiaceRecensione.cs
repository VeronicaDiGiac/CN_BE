using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // "Mi piace" di un utente su una recensione. Chiave composta (IdUtente, IdRecensione)
    // configurata in CinemaContext: un utente mette al massimo un "mi piace" per recensione.
    [Table("MiPiaceRecensioni")]
    public class MiPiaceRecensione
    {
        public int IdUtente { get; set; }
        public int IdRecensione { get; set; }
        public DateTime DataMiPiace { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdRecensione")]
        public Recensione? Recensione { get; set; }
    }
}
