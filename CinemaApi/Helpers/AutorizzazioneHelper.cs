using CinemaApi.Models;
using System.Data;

namespace CinemaApi.Helpers
{
    public class AutorizzazioneHelper
    {
        public static bool PuoModificare ( int? idProprietarioRisorsa, int idUtenteAutenticato, string ruolo)
        {
            return idProprietarioRisorsa == idUtenteAutenticato || ruolo == Ruoli.Admin;
        }

        // I film sono schede condivise: dopo l'approvazione ci sono recensioni, liste e discussioni
        // di altri utenti. Chi li ha proposti può correggerli o ritirarli solo finché sono in attesa;
        // poi solo l'admin (le richieste degli utenti arrivano come segnalazioni di tipo Film).
        public static bool PuoModificareFilm(Film film, int idUtenteAutenticato, string ruolo) =>
            ruolo == Ruoli.Admin || (film.IdUtente == idUtenteAutenticato && film.Stato == StatiFilm.InAttesa);
    }
}
