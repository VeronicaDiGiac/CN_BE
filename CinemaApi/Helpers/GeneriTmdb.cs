namespace CinemaApi.Helpers
{
    // Un genere come lo sceglie l'utente nei filtri, con i corrispondenti id TMDB.
    // Film e serie su TMDB hanno elenchi diversi (es. "Azione" per i film, "Action & Adventure" per le serie):
    // IdSerie/NomeSerie collegano le due cose; IdSerie null = per le serie quel genere non esiste.
    public record GenereTmdb(string Nome, int IdFilm, int? IdSerie, string? NomeSerie = null);

    public static class GeneriTmdb
    {
        // Elenco fisso: gli id dei generi TMDB non cambiano. Nomi come in TMDB it-IT (sono quelli
        // salvati in Film.Genere quando un titolo si importa da TMDB)
        public static readonly IReadOnlyList<GenereTmdb> Tutti = new List<GenereTmdb>
        {
            new("Animazione", 16, 16),
            new("Avventura", 12, 10759, "Action & Adventure"),
            new("Azione", 28, 10759, "Action & Adventure"),
            new("Commedia", 35, 35),
            new("Crime", 80, 80),
            new("Documentario", 99, 99),
            new("Dramma", 18, 18),
            new("Famiglia", 10751, 10751),
            new("Fantascienza", 878, 10765, "Sci-Fi & Fantasy"),
            new("Fantasy", 14, 10765, "Sci-Fi & Fantasy"),
            new("Guerra", 10752, 10768, "War & Politics"),
            new("Horror", 27, null),
            new("Mistero", 9648, 9648),
            new("Musica", 10402, null),
            new("Romance", 10749, null),
            new("Storia", 36, null),
            new("Thriller", 53, null),
            new("Western", 37, 37),
        };

        public static GenereTmdb? Trova(string? nome) =>
            string.IsNullOrWhiteSpace(nome) ? null
            : Tutti.FirstOrDefault(g => string.Equals(g.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
