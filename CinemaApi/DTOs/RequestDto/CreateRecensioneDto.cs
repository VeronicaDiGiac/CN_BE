using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateRecensioneDto
    {
        // NB: niente IdUtente qui. L'autore si legge dal token JWT nel
        // controller, mai dal client (stesso principio di tutti gli altri Create).

        [Required]
        public int IdFilm { get; set; }

        [Range(1, 5)]
        public int? Voto { get; set; }

        [MaxLength(5000)]
        public string? Contenuto { get; set; }
    }
}