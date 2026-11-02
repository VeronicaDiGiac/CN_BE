using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    // Abbinamento confermato dall'admin tra un film del catalogo e un film TMDB
    public class CollegaTmdbDto
    {
        [Required]
        public int IdFilm { get; set; }

        [Required]
        public int TmdbId { get; set; }
    }
}
