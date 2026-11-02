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
        // Se true il FE nasconde il testo finché l'utente non sceglie di leggerlo
        public bool Spoiler { get; set; }

        public ICollection<MiPiaceRecensione>? MiPiace { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }

        public ICollection<Commento>? Commenti { get; set; }
    }
}