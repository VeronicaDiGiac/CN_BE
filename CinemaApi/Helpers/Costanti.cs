namespace CinemaApi.Helpers
{
    // Valori ammessi dai CHECK constraint del DB: usarli da qui evita errori di battitura
    // ("Approvato" vs "approvato") che il compilatore non vedrebbe.

    public static class Ruoli
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    public static class StatiFilm
    {
        public const string Approvato = "Approvato";
        public const string InAttesa = "InAttesa";
        public const string Rifiutato = "Rifiutato";
    }

    // Tipo di titolo del catalogo (CHECK constraint di Film.Tipo)
    public static class TipiTitolo
    {
        public const string Film = "Film";
        public const string Serie = "Serie";

        // Valore della query string (?tipo=serie, ?tipo=Film…) nella forma salvata nel DB;
        // null se non è un tipo valido
        public static string? Normalizza(string? valore) =>
            string.Equals(valore, Film, StringComparison.OrdinalIgnoreCase) ? Film
            : string.Equals(valore, Serie, StringComparison.OrdinalIgnoreCase) ? Serie
            : null;
    }

    // A che punto è un utente con una serie (calcolato, non salvato)
    public static class StatiSerie
    {
        public const string NonIniziata = "NonIniziata";
        public const string InCorso = "InCorso";
        // Visti tutti gli episodi usciti, ma la serie continua
        public const string InPari = "InPari";
        // Visti tutti gli episodi e la serie è finita
        public const string Completata = "Completata";

        public static string Calcola(int episodiVisti, int episodiUsciti, bool conclusa) =>
            episodiVisti == 0 ? NonIniziata
            : episodiVisti < episodiUsciti ? InCorso
            : conclusa ? Completata
            : InPari;
    }

    // Tipi di voce del feed delle attività
    public static class TipiAttivita
    {
        public const string Visione = "Visione";
        public const string Recensione = "Recensione";
    }

    // Valori ammessi dal CHECK constraint di Notifiche.Tipo
    public static class TipiNotifica
    {
        public const string NuovoFollower = "NuovoFollower";
        public const string MiPiaceRecensione = "MiPiaceRecensione";
        public const string CommentoRecensione = "CommentoRecensione";
        public const string RispostaCommento = "RispostaCommento";
        // Di sistema (senza mittente): è uscito un episodio di una serie che si sta guardando
        public const string NuovoEpisodio = "NuovoEpisodio";
        public const string MiPiaceCommento = "MiPiaceCommento";
        public const string MiPiaceDiscussione = "MiPiaceDiscussione";
        // Qualcuno ha scritto nella tua discussione
        public const string CommentoDiscussione = "CommentoDiscussione";
    }

    // Elenchi presi da TMDB per il catalogo (valori usati nell'URL: /api/tmdb/elenco/{categoria})
    public static class CategorieTmdb
    {
        // Film
        public const string AlCinema = "alcinema";
        public const string Tendenze = "tendenze";
        public const string Popolari = "popolari";
        public const string InArrivo = "inarrivo";
        // Serie TV
        public const string SerieTendenze = "serie-tendenze";
        public const string SeriePopolari = "serie-popolari";
        public const string SerieInOnda = "serie-inonda";

        public static readonly string[] Tutte = { AlCinema, Tendenze, Popolari, InArrivo, SerieTendenze, SeriePopolari, SerieInOnda };
    }

    // Ordinamenti dell'elenco del catalogo (GET /api/film?ordinamento=)
    public static class OrdinamentiCatalogo
    {
        public const string Recenti = "recenti";
        public const string Voto = "voto";
        public const string Titolo = "titolo";
    }

    public static class TipiContenuto
    {
        public const string Recensione = "Recensione";
        public const string Commento = "Commento";
        public const string Discussione = "Discussione";
        // Problema su una scheda film (dati sbagliati, doppione da eliminare…)
        public const string Film = "Film";
    }

    // Ordinamenti delle discussioni (GET /api/discussioni/film/{id}?ordinamento=)
    public static class OrdinamentiDiscussioni
    {
        public const string Recenti = "recenti";
        public const string Apprezzate = "apprezzate";
    }
}
