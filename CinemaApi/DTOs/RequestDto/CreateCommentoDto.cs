using System.ComponentModel.DataAnnotations;
using CinemaApi.Helpers;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateCommentoDto
    {
        // Esattamente uno dei due: sotto una recensione oppure in una discussione
        public int? IdRecensione { get; set; }
        public int? IdDiscussione { get; set; }

        // Breve: al massimo 100 parole e 500 caratteri (la colonna nel DB è nvarchar(500))
        [Required]
        [MaxLength(500)]
        [Parole(Massimo = LimitiParole.CommentoMassimo)]
        public string? Contenuto { get; set; }

        public int? IdCommentoPadre { get; set; }

        public bool Spoiler { get; set; }
    }
}