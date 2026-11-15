using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // Scelta dell'utente sugli avvisi delle nuove puntate di una serie (tabella SerieSeguite).
    // Senza riga: avvisi solo se ha visto almeno un episodio. Avvisi = true: li vuole comunque
    // ("Segui la serie"); Avvisi = false: non li vuole anche se la sta guardando.
    [Table("SerieSeguite")]
    public class SerieSeguita
    {
        public int IdUtente { get; set; }
        public int IdFilm { get; set; }
        public bool Avvisi { get; set; }
        public DateTime DataModifica { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }
    }
}
