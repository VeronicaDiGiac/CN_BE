using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    // Token di rinnovo ricevuto all'accesso: serve per una sessione nuova (rinnova) o per chiuderla (esci)
    public class TokenRinnovoDto
    {
        [Required]
        [MaxLength(200)]
        public string TokenRinnovo { get; set; } = "";
    }
}
