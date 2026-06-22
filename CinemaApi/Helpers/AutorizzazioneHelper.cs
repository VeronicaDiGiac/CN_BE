using System.Data;

namespace CinemaApi.Helpers
{
    public class AutorizzazioneHelper
    {
        public static bool PuoModificare ( int? idProprietarioRisorsa, int idUtenteAutenticato, string ruolo)
        {
            return idProprietarioRisorsa == idUtenteAutenticato || ruolo == "Admin";
        }
    }
}
