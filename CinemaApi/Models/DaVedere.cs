using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // Un film nella lista "Da vedere" di un utente (al massimo una volta per utente)
    public class DaVedere
    {
        [Key]
        public int IdDaVedere { get; set; }
        public int IdUtente { get; set; }
        public int IdFilm { get; set; }
        public DateTime DataAggiunta { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }
    }
}
