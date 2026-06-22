using CinemaApi.Models;
using System.ComponentModel.DataAnnotations;

namespace CinemaApi.Models
{
    public class Utente
    {
        [Key]
        public int IdUtente { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Ruolo { get; set; }
        public DateTime? DataDiRegistrazione { get; set; }
        public string? PasswordHash { get; set; }

        // Navigation properties
        public ICollection<Recensione>? Recensioni { get; set; }
        public ICollection<Commento>? Commenti { get; set; }
        public ICollection<Preferito>? Preferiti { get; set; }
    }
}