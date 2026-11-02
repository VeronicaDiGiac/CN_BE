using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CinemaApi.Helpers
{
    // Piccoli controlli ripetuti in più controller e service, scritti una volta sola
    public static class EstensioniCinema
    {
        // Id dell'utente autenticato, letto dal token JWT (solo nelle azioni con [Authorize])
        public static int IdUtente(this ClaimsPrincipal utente) =>
            int.Parse(utente.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Id dell'utente se la richiesta ha un token valido, altrimenti null (azioni pubbliche)
        public static int? IdUtenteOpzionale(this ClaimsPrincipal utente) =>
            int.TryParse(utente.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        public static string Ruolo(this ClaimsPrincipal utente) =>
            utente.FindFirst(ClaimTypes.Role)!.Value;

        // Solo i film approvati sono visibili: recensioni, preferiti, visioni ecc. si fanno solo su quelli
        public static Task<bool> FilmApprovatoEsiste(this CinemaContext db, int idFilm) =>
            db.Film.AnyAsync(f => f.IdFilm == idFilm && f.Stato == StatiFilm.Approvato);
    }
}
