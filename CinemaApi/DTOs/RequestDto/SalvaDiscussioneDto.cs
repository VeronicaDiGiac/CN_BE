using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    // Nuova discussione o modifica. In modifica film ed episodio non cambiano (si ignorano).
    public class SalvaDiscussioneDto
    {
        [Required]
        public int IdFilm { get; set; }

        [Required]
        [MaxLength(200)]
        public string Titolo { get; set; } = "";

        [MaxLength(4000)]
        public string? Contenuto { get; set; }

        public bool Spoiler { get; set; }

        // Solo per le serie: entrambi o nessuno
        [Range(0, 1000)]
        public int? Stagione { get; set; }
        [Range(1, 10000)]
        public int? Episodio { get; set; }
    }
}
