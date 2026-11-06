using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    // Episodio di cui si vuole il filo dei commenti (POST /api/discussioni/episodio)
    public class FiloEpisodioDto
    {
        [Required]
        public int IdFilm { get; set; }
        [Range(0, 1000)]
        public int Stagione { get; set; }
        [Range(1, 10000)]
        public int Episodio { get; set; }
    }
}
