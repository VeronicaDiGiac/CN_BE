using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CambiaPasswordDto
    {
        // La password attuale serve a confermare che chi cambia la password
        // sia davvero il proprietario dell'account (non basta il token).
        [Required]
        public string PasswordAttuale { get; set; } = "";

        // Stessa regola della registrazione (RegisterDto)
        [Required]
        [MinLength(6)]
        public string NuovaPassword { get; set; } = "";

        // NB: niente IdUtente qui. L'utente si legge dal token JWT nel controller.
    }
}
