using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class UpdateStatoSegnalazioneDto
    {
        [Required]
        [MaxLength(20)]
        // Stessi valori del CHECK constraint sul DB: qui l'errore diventa un 400 leggibile
        [RegularExpression("^(Aperta|Gestita|Respinta)$", ErrorMessage = "Stato non valido: usa Aperta, Gestita o Respinta")]
        public string Stato { get; set; } = "";   // "Gestita" o "Respinta"
    }
}