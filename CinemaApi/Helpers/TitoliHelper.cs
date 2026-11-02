using System.Globalization;
using System.Text;

namespace CinemaApi.Helpers
{
    public static class TitoliHelper
    {
        // Solo lettere e cifre, minuscole e senza accenti: "Dune: Parte Due" -> "dunepartedue".
        // Due titoli con la stessa chiave sono considerati lo stesso film (se l'anno è compatibile).
        public static string Chiave(string titolo)
        {
            var scomposto = titolo.Normalize(NormalizationForm.FormD);
            var lettere = scomposto.Where(c =>
                CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c));
            var chiave = new string(lettere.ToArray()).ToLowerInvariant();
            return chiave.Length <= 200 ? chiave : chiave[..200];
        }
    }
}
