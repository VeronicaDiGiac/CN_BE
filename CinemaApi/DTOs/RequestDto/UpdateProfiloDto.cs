using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class UpdateProfiloDto
    {
        [MaxLength(50)]
        //Questo ha bisogno di alcuni controlli e implementazioni per non far cambiare selvaggiamente il nome all'utente 
        public string? UserName { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }
    }
}