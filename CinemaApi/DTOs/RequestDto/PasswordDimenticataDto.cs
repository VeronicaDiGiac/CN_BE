using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class PasswordDimenticataDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}
