using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    [Table("Notifiche")]
    public class Notifica
    {
        [Key]
        public int IdNotifica { get; set; }
        public int IdDestinatario { get; set; }
        // null per le notifiche di sistema (nuovo episodio)
        public int? IdMittente { get; set; }
        // Uno dei valori di Helpers.TipiNotifica
        public required string Tipo { get; set; }
        public int? IdFilm { get; set; }
        public int? IdRecensione { get; set; }
        public int? IdCommento { get; set; }
        public int? IdDiscussione { get; set; }
        // Solo per NuovoEpisodio
        public int? Stagione { get; set; }
        public int? Episodio { get; set; }
        public bool Letta { get; set; }
        public DateTime DataCreazione { get; set; }

        [ForeignKey("IdMittente")]
        public Utente? Mittente { get; set; }
    }
}
