using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // Token di rinnovo della sessione (tabella TokenRinnovo): il JWT dura poco, questo dura 30 giorni
    // e serve solo a chiederne uno nuovo. Nel DB c'è l'hash, mai il token. Ogni rinnovo lo sostituisce
    // (vale una volta sola); logout e cambio password lo cancellano.
    [Table("TokenRinnovo")]
    public class TokenRinnovo
    {
        [Key]
        public int IdTokenRinnovo { get; set; }
        public int IdUtente { get; set; }
        public string TokenHash { get; set; } = "";
        public DateTime DataCreazione { get; set; }
        public DateTime Scadenza { get; set; }

        [ForeignKey("IdUtente")]
        public Utente? Utente { get; set; }
    }
}
