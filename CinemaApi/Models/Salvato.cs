using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaApi.Models
{
    // Una recensione o una discussione salvata da un utente (segnalibro).
    // Chiave (IdUtente, TipoContenuto, IdContenuto) in CinemaContext; tabella creata da Database/Salvati.sql
    [Table("Salvati")]
    public class Salvato
    {
        public int IdUtente { get; set; }
        // TipiContenuto.Recensione o TipiContenuto.Discussione
        public string TipoContenuto { get; set; } = "";
        public int IdContenuto { get; set; }
        public DateTime DataSalvataggio { get; set; }
    }
}
