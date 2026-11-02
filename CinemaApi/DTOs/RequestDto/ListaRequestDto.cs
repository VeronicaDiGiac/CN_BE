using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    // Creazione e modifica di una lista (limiti uguali alle colonne di Database/Liste.sql)
    public class SalvaListaDto
    {
        [Required]
        [MaxLength(100)]
        public string Titolo { get; set; } = "";

        [MaxLength(1000)]
        public string? Descrizione { get; set; }

        public bool Pubblica { get; set; } = true;
    }

    public class AggiungiFilmListaDto
    {
        [MaxLength(300)]
        public string? Nota { get; set; }
    }

    // Nuovo ordine dei film: tutti gli id della lista, nell'ordine voluto
    public class OrdineListaDto
    {
        [Required]
        public List<int> IdFilm { get; set; } = new();
    }
}
