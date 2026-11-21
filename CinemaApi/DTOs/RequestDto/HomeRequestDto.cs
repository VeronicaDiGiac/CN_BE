using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    // Risposta scelta in un sondaggio
    public class VotoSondaggioDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Scegli una risposta")]
        public int IdOpzione { get; set; }
    }

    // Sondaggio nuovo dal pannello admin
    public class CreaSondaggioDto
    {
        [Required(ErrorMessage = "Scrivi la domanda")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "La domanda va da 5 a 200 caratteri")]
        public string Domanda { get; set; } = "";

        // Quando parte; se manca, subito
        public DateTime? DataInizio { get; set; }

        [MinLength(2, ErrorMessage = "Servono almeno 2 risposte")]
        [MaxLength(4, ErrorMessage = "Al massimo 4 risposte")]
        public List<OpzioneSondaggioRequestDto> Opzioni { get; set; } = new();
    }

    public class OpzioneSondaggioRequestDto
    {
        [Required(ErrorMessage = "Ogni risposta ha bisogno di un testo")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Ogni risposta va da 1 a 100 caratteri")]
        public string Testo { get; set; } = "";

        // Titolo del catalogo da mostrare con la locandina (facoltativo)
        public int? IdFilm { get; set; }
    }

    // Film club scelto dall'admin per la settimana in corso o per la prossima
    public class ImpostaFilmClubDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Scegli un titolo")]
        public int IdFilm { get; set; }

        // true: la prossima settimana; false: quella in corso
        public bool ProssimaSettimana { get; set; }
    }
}
