using CinemaApi.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    public class Recensione
    {
        [Key]
        public int IdRecensione { get; set; }
        public int IdUtente { get; set; }
        public int IdFilm { get; set; }
        public int? Voto { get; set; }
        public string? Contenuto { get; set; }
        public DateTime? DataRecensione { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }

        public ICollection<Commento>? Commenti { get; set; }
    }
}