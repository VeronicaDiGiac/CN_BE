using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateSegnalazioneDto
    {
        // "Recensione", "Commento", "Discussione" o "Film". Validato anche lato DB dal CHECK constraint.
        [Required]
        [MaxLength(20)]
        [RegularExpression("^(Recensione|Commento|Discussione|Film)$", ErrorMessage = "TipoContenuto non valido: usa Recensione, Commento, Discussione o Film")]
        public string TipoContenuto { get; set; } = "";

        [Required]
        public int IdContenuto { get; set; }

        [MaxLength(500)]
        public string? Motivo { get; set; }

        // NB: niente IdUtente qui. Chi segnala si legge dal token JWT nel
        // controller, mai dal client (stesso principio di tutti gli altri Create).
    }
}