using System.ComponentModel.DataAnnotations;
using CinemaApi.Helpers;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateRecensioneDto
    {
        // NB: niente IdUtente qui. L'autore si legge dal token JWT nel
        // controller, mai dal client (stesso principio di tutti gli altri Create).

        [Required]
        public int IdFilm { get; set; }

        // Facoltativo: se c'è diventa (o cambia) il voto dell'autore al film, in VotiFilm
        [Range(1, 5)]
        public int? Voto { get; set; }

        // Una recensione è un testo vero: da 30 a 1000 parole (un commento ne ha al massimo 100).
        // 12000 caratteri bastano per 1000 parole anche lunghe; la colonna nel DB è nvarchar(max)
        [Required(ErrorMessage = "Scrivi il testo della recensione")]
        [MaxLength(12000)]
        [Parole(Minimo = LimitiParole.RecensioneMinimo, Massimo = LimitiParole.RecensioneMassimo)]
        public string? Contenuto { get; set; }

        public bool Spoiler { get; set; }
    }
}