using CinemaApi.Models;
using System.Linq.Expressions;

namespace CinemaApi.DTOs.ResponseDto
{
    // Film come lo vede il FE (catalogo, scheda da modificare, film in attesa).
    // Al posto dell'entità: niente campi interni (TitoloChiave) e niente navigation property.
    public class FilmDto
    {
        public int IdFilm { get; set; }
        // Chi ha inserito il film (serve per i permessi di modifica)
        public int? IdUtente { get; set; }
        public string? Titolo { get; set; }
        public int? AnnoUscita { get; set; }
        public string? Regista { get; set; }
        public string? Genere { get; set; }
        public string? Attori { get; set; }
        public string? ImmagineUrl { get; set; }
        public string? Descrizione { get; set; }
        public string Stato { get; set; } = "";
        public int? DurataMinuti { get; set; }
        public int? TmdbId { get; set; }
        public double? VotoMedio { get; set; }
        public int NumeroRecensioni { get; set; }

        // Proiezione usata nelle query EF (diventa SQL, non carica le recensioni)
        public static readonly Expression<Func<Film, FilmDto>> DaFilm = f => new FilmDto
        {
            IdFilm = f.IdFilm,
            IdUtente = f.IdUtente,
            Titolo = f.Titolo,
            AnnoUscita = f.AnnoUscita,
            Regista = f.Regista,
            Genere = f.Genere,
            Attori = f.Attori,
            ImmagineUrl = f.ImmagineUrl,
            Descrizione = f.Descrizione,
            Stato = f.Stato,
            DurataMinuti = f.DurataMinuti,
            TmdbId = f.TmdbId,
            VotoMedio = f.Recensioni!.Average(r => (double?)r.Voto),
            NumeroRecensioni = f.Recensioni!.Count()
        };
    }
}
