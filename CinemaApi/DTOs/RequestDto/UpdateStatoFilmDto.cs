using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class UpdateStatoFilmDto
    {
        [Required]
        [MaxLength(20)]
        public string Stato { get; set; } = "";
    }
}