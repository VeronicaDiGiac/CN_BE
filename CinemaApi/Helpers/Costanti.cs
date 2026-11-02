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

    public static class TipiContenuto
    {
        public const string Recensione = "Recensione";
        public const string Commento = "Commento";
    }
}
