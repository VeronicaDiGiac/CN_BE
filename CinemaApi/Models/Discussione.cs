using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // Un argomento di discussione su un film o una serie (o su un episodio, con Stagione/Episodio).
    // I messaggi sono i Commenti con IdDiscussione.
    [Table("Discussioni")]
    public class Discussione
    {
        [Key]
        public int IdDiscussione { get; set; }
        public int IdFilm { get; set; }
        public int IdUtente { get; set; }
        public required string Titolo { get; set; }
        public string? Contenuto { get; set; }
        public bool Spoiler { get; set; }
        public int? Stagione { get; set; }
        public int? Episodio { get; set; }
        public DateTime DataCreazione { get; set; }
        public DateTime DataUltimaAttivita { get; set; }
        // Filo dei commenti di un episodio (Database/FiliEpisodio.sql): uno per episodio, creato al primo
        // commento, senza titolo scelto dall'utente; non compare nell'elenco delle discussioni
        public bool FiloEpisodio { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        public ICollection<Commento>? Commenti { get; set; }
        public ICollection<MiPiaceDiscussione>? MiPiace { get; set; }
    }

    // "Mi piace" su una discussione: chiave (IdUtente, IdDiscussione) in CinemaContext
    [Table("MiPiaceDiscussioni")]
    public class MiPiaceDiscussione
    {
        public int IdUtente { get; set; }
        public int IdDiscussione { get; set; }
        public DateTime DataMiPiace { get; set; }

        [ForeignKey("IdDiscussione")]
        public Discussione? Discussione { get; set; }
    }

    // "Mi piace" su un commento (di una recensione o di una discussione): chiave (IdUtente, IdCommento)
    [Table("MiPiaceCommenti")]
    public class MiPiaceCommento
    {
        public int IdUtente { get; set; }
        public int IdCommento { get; set; }
        public DateTime DataMiPiace { get; set; }

        [ForeignKey("IdCommento")]
        public Commento? Commento { get; set; }
    }
}
