using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateFilmDto
    {
        [Required]
        [MaxLength(50)]
        public string? Titolo { get; set; }

        [Range(1888, 2100)]
        public int? AnnoUscita { get; set; }

        [MaxLength(50)]
        public string? Regista { get; set; }

        [MaxLength(50)]
        public string? Genere { get; set; }

        [MaxLength(200)]
        public string? Attori { get; set; }

        [MaxLength(500)]
        public string? ImmagineUrl { get; set; }

        [MaxLength(1000)]
        public string? Descrizione { get; set; }
    }
}