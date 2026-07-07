using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class UpdateStatoSegnalazioneDto
    {
        [Required]
        [MaxLength(20)]
        public string Stato { get; set; } = "";   // "Gestita" o "Respinta"
    }
}