using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // Stelle (da 1 a 5) che un utente dà alla recensione di un altro: dicono quanto è scritta bene,
    // non quanto è bello il film. Chiave (IdUtente, IdRecensione) configurata in CinemaContext.
    [Table("VotiRecensioni")]
    public class VotoRecensione
    {
        public int IdUtente { get; set; }
        public int IdRecensione { get; set; }
        public int Voto { get; set; }
        public DateTime DataVoto { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdRecensione")]
        public Recensione? Recensione { get; set; }
    }
}
