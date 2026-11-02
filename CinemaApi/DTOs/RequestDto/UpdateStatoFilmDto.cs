using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class UpdateStatoFilmDto
    {
        [Required]
        [MaxLength(20)]
        // Stessi valori del CHECK constraint sul DB: qui l'errore diventa un 400 leggibile
        [RegularExpression("^(Approvato|InAttesa|Rifiutato)$", ErrorMessage = "Stato non valido: usa Approvato, InAttesa o Rifiutato")]
        public string Stato { get; set; } = "";
    }
}