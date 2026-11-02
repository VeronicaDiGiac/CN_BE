using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateVisioneDto
    {
        [Required]
        public int IdFilm { get; set; }

        // Se manca vale oggi. Non può essere nel futuro (controllo nel controller).
        public DateOnly? DataVisione { get; set; }
    }
}
