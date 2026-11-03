using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    // Accesso con Google: l'ID token che Auth0 ha dato al FE
    public class AccessoEsternoDto
    {
        [Required]
        [MaxLength(8000)]
        public string IdToken { get; set; } = "";
    }
}
