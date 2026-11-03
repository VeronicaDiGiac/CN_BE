using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // Un episodio di una serie che l'utente ha visto (gli episodi in sé si leggono da TMDB)
    [Table("EpisodiVisti")]
    public class EpisodioVisto
    {
        [Key]
        public int IdEpisodioVisto { get; set; }
        public int IdUtente { get; set; }
        // La serie (riga di Film con Tipo = Serie)
        public int IdFilm { get; set; }
        // 0 = episodi speciali
        public int Stagione { get; set; }
        public int Episodio { get; set; }
        public DateOnly DataVisione { get; set; }
        public int? DurataMinuti { get; set; }
        public DateTime DataCreazione { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }
    }
}
