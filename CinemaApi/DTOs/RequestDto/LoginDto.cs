using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        // Niente [MinLength] qui: questo è un DTO di LOGIN, non di registrazione.
        // Se in futuro cambiassimo il requisito minimo per le NUOVE password,
        // utenti già registrati con password più corte (create prima del cambio)
        // non potrebbero più fare login pur avendo la password corretta.
        // Il controllo di lunghezza minima va solo su RegisterDto, al momento
        // della creazione della password.
        public string Password { get; set; }
    }
}