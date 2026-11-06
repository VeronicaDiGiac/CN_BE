using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // Una voce del diario: l'utente ha visto il film in quella data (si può rivedere più volte).
    // La data è facoltativa (Database/VisioneSenzaData.sql): null se l'utente non ha voluto indicarla
    public class Visione
    {
        [Key]
        public int IdVisione { get; set; }
        public int IdUtente { get; set; }
        public int IdFilm { get; set; }
        public DateOnly? DataVisione { get; set; }
        public DateTime DataCreazione { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }
    }
}
