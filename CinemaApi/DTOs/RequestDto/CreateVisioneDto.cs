using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateVisioneDto
    {
        [Required]
        public int IdFilm { get; set; }

        // Se manca vale oggi (es. il flag rapido sulle copertine). Non può essere nel futuro.
        public DateOnly? DataVisione { get; set; }

        // true: l'utente non vuole indicare quando l'ha visto (la data resta vuota, DataVisione si ignora)
        public bool SenzaData { get; set; }
    }

    // Cambio della data di un film già visto (dalla scheda): stessa regola della creazione
    public class DataVisioneDto
    {
        public DateOnly? DataVisione { get; set; }
        public bool SenzaData { get; set; }
    }
}
