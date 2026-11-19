using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // Voto da 1 a 5 stelle di un utente a un film o a una serie (tabella VotiFilm).
    // È separato dalla recensione: si può votare senza scrivere niente. Chiave (IdUtente, IdFilm)
    // configurata in CinemaContext: un voto per utente e film, si cambia sovrascrivendolo.
    [Table("VotiFilm")]
    public class VotoFilm
    {
        public int IdUtente { get; set; }
        public int IdFilm { get; set; }
        public int Voto { get; set; }
        public DateTime DataVoto { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }
    }
}
