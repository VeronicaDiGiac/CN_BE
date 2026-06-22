using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateRecensioneDto
    {
        [Required]
        public int IdUtente { get; set; }

        [Required]
        public int IdFilm { get; set; }

        [Range(1, 5)]
        public int? Voto { get; set; }

        [MaxLength(5000)]
        public string? Contenuto { get; set; }
    }
}