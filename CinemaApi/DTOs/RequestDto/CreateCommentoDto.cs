using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateCommentoDto
    {
        [Required]
        public int IdRecensione { get; set; }

        [Required]
        [MaxLength(500)]
        public string? Contenuto { get; set; }

        public int? IdCommentoPadre { get; set; }
    }
}