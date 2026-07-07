using CinemaApi.Models;
using System.ComponentModel.DataAnnotations;

namespace CinemaApi.Models
{
    public class Film
    {
        [Key]
        public int IdFilm { get; set; }

        //IdUtente è nullable solo per ora
        public int? IdUtente { get; set; }
        public string? Titolo { get; set; }
        public int? AnnoUscita { get; set; }
        public string? Regista { get; set; }
        public string? Genere { get; set; }
        public string? Attori { get; set; }

        public string? ImmagineUrl { get; set; }

        public string? Descrizione { get; set; }

        public string Stato { get; set; } = "InAttesa";

        // Navigation properties
        public ICollection<Recensione>? Recensioni { get; set; }
        public ICollection<Preferito>? Preferiti { get; set; }
    }
}