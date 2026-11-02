using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    [Table("Liste")]
    public class Lista
    {
        [Key]
        public int IdLista { get; set; }
        public int IdUtente { get; set; }
        public required string Titolo { get; set; }
        public string? Descrizione { get; set; }
        public bool Pubblica { get; set; }
        public DateTime DataCreazione { get; set; }
        public DateTime DataModifica { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }

        public ICollection<ListaFilm>? Film { get; set; }
    }

    // Film in una lista. Chiave composta (IdLista, IdFilm) configurata in CinemaContext.
    [Table("ListeFilm")]
    public class ListaFilm
    {
        public int IdLista { get; set; }
        public int IdFilm { get; set; }
        public int Posizione { get; set; }
        public string? Nota { get; set; }
        public DateTime DataAggiunta { get; set; }

        [ForeignKey("IdLista")]
        public Lista? Lista { get; set; }

        [ForeignKey("IdFilm")]
        public Film? Film { get; set; }
    }
}
