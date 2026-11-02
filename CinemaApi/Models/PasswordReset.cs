using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // Richiesta di "Password dimenticata?": il token in chiaro va solo nella mail,
    // qui si salva l'hash SHA-256 (come per le password, un DB letto non basta per usarlo)
    public class PasswordReset
    {
        [Key]
        public int IdPasswordReset { get; set; }
        public int IdUtente { get; set; }
        public required string TokenHash { get; set; }
        public DateTime DataCreazione { get; set; }
        public DateTime Scadenza { get; set; }
        public bool Usato { get; set; }

        // Navigation property
        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }
    }
}
