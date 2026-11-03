using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateCommentoDto
    {
        // Esattamente uno dei due: sotto una recensione oppure in una discussione
        public int? IdRecensione { get; set; }
        public int? IdDiscussione { get; set; }

        [Required]
        [MaxLength(500)]
        public string? Contenuto { get; set; }

        public int? IdCommentoPadre { get; set; }

        public bool Spoiler { get; set; }
    }
}