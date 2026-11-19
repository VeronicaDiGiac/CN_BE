using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    // Stelle date a un film o a una recensione
    public class VotoDto
    {
        [Range(1, 5, ErrorMessage = "Il voto va da 1 a 5 stelle")]
        public int Voto { get; set; }
    }
}
