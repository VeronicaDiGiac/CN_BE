using System.Text.RegularExpressions;

namespace CinemaApi.Helpers
{
    // Riconosce se un testo (una trama) è in inglese invece che in italiano.
    // TMDB, quando non ha la trama in italiano, dà quella inglese: la scheda lo deve dire.
    // Si contano le parole più comuni delle due lingue: su un testo di qualche riga basta.
    public static partial class LinguaTesto
    {
        private static readonly HashSet<string> Inglesi = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "of", "to", "is", "his", "her", "with", "who", "when", "their", "from",
            "for", "on", "an", "in", "as", "by", "that", "this", "after", "into", "while", "must"
        };

        private static readonly HashSet<string> Italiane = new(StringComparer.OrdinalIgnoreCase)
        {
            "il", "lo", "la", "gli", "le", "di", "che", "è", "e", "del", "della", "un", "una",
            "per", "con", "nel", "nella", "dopo", "suo", "sua", "loro", "quando", "alla", "dei", "delle"
        };

        // Sotto queste parole riconosciute il testo è troppo corto per decidere: si considera italiano
        private const int ParoleMinime = 4;

        public static bool SembraInglese(string? testo)
        {
            if (string.IsNullOrWhiteSpace(testo)) return false;
            int inglesi = 0, italiane = 0;
            foreach (Match parola in Parole().Matches(testo))
            {
                if (Inglesi.Contains(parola.Value)) inglesi++;
                else if (Italiane.Contains(parola.Value)) italiane++;
            }
            return inglesi + italiane >= ParoleMinime && inglesi > italiane * 2;
        }

        [GeneratedRegex(@"\p{L}+")]
        private static partial Regex Parole();
    }
}
