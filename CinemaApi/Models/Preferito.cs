using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    public class Preferito
    {
        [Key]
        public int IdPreferito { get; set; }
        public int IdUtente { get; set; }
        public int IdFilm { get; set; }
        public DateTime? DataPreferito { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }
    }
}