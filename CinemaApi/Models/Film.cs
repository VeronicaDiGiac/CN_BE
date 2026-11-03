using CinemaApi.Models;
using System.ComponentModel.DataAnnotations;

namespace CinemaApi.Models
{
    public class Film
    {
        [Key]
        public int IdFilm { get; set; }

        //IdUtente è nullable solo per ora
        public int? IdUtente { get; set; }
        public string? Titolo { get; set; }
        public int? AnnoUscita { get; set; }
        public string? Regista { get; set; }
        public string? Genere { get; set; }
        public string? Attori { get; set; }

        public string? ImmagineUrl { get; set; }

        public string? Descrizione { get; set; }

        public string Stato { get; set; } = "InAttesa";

        // "Film" o "Serie" (Helpers.TipiTitolo): le serie TV stanno nella stessa tabella
        public string Tipo { get; set; } = "Film";

        // Solo per le serie: numero di stagioni secondo TMDB
        public int? NumeroStagioni { get; set; }

        // Minuti, da TMDB (null se non si conosce): serve alle statistiche "ore di cinema"
        public int? DurataMinuti { get; set; }

        // Id del film su TMDB: null per i film inseriti a mano
        public int? TmdbId { get; set; }

        // Titolo normalizzato per riconoscere i doppioni (TitoliHelper.Chiave).
        // Non va impostato a mano: lo calcola CinemaContext a ogni salvataggio.
        public string? TitoloChiave { get; set; }

        // Navigation properties
        public ICollection<Recensione>? Recensioni { get; set; }
        public ICollection<Preferito>? Preferiti { get; set; }
    }
}