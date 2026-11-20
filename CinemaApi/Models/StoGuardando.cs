using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // "Sto guardando" (tabella StoGuardando): cosa sta guardando l'utente in questo momento.
    // Lo decide lui dalla scheda (mai automatico), una riga per utente. Vale fino a Fine
    // (durata del film o di un episodio, più un margine) o finché non segna il film come visto
    // o lo toglie. Lo vedono solo lui e chi lo segue.
    [Table("StoGuardando")]
    public class StoGuardando
    {
        [Key]
        public int IdUtente { get; set; }
        public int IdFilm { get; set; }
        public DateTime Inizio { get; set; }
        public DateTime Fine { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }
    }
}
