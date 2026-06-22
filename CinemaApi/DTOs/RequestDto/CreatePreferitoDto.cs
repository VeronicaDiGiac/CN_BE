using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreatePreferitoDto
    {
        [Required]
        public int IdFilm { get; set; }
    }
}