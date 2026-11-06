using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateVisioneDto
    {
        [Required]
        public int IdFilm { get; set; }

        // La data di visione non si sceglie più (decisione del 2026-11-06): il BE salva quella di oggi.
        // Per rimetterla basta togliere i commenti qui, in VisioniController.Create e in VisioniService.CreateVisione.
        // // Se manca vale oggi. Non può essere nel futuro (controllo nel controller).
        // public DateOnly? DataVisione { get; set; }
        //
        // // true: l'utente non vuole indicare quando l'ha visto (la data resta vuota, DataVisione si ignora)
        // public bool SenzaData { get; set; }
    }
}
