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
    }

    // Elenchi di film presi da TMDB per il catalogo (valori usati nell'URL: /api/tmdb/elenco/{categoria})
    public static class CategorieTmdb
    {
        public const string AlCinema = "alcinema";
        public const string Tendenze = "tendenze";
        public const string Popolari = "popolari";
        public const string InArrivo = "inarrivo";

        public static readonly string[] Tutte = { AlCinema, Tendenze, Popolari, InArrivo };
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
    }
}
