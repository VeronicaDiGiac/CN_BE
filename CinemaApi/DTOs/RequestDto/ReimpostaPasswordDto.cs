using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class ReimpostaPasswordDto
    {
        // Il token arriva dal link nella mail (…/reimposta-password?token=…)
        [Required]
        public string Token { get; set; } = "";

        // Stessa regola della registrazione (RegisterDto)
        [Required]
        [MinLength(6)]
        public string NuovaPassword { get; set; } = "";
    }
}
