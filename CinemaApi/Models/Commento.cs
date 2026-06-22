using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    public class Commento
    {
        [Key]
        public int IdCommento { get; set; }
        public int IdUtente { get; set; }
        public int IdRecensione { get; set; }
        public string? Contenuto { get; set; }
        public DateTime? DataCommento { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        [ForeignKey("IdRecensione")]
        public Recensione? Recensione { get; set; }

        public int? IdCommentoPadre { get; set; }

        [ForeignKey("IdCommentoPadre")]
        public Commento? CommentoPadre { get; set; }

        public ICollection<Commento>? Risposte { get; set; }
    }
}