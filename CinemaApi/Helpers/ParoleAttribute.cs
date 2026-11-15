using System.ComponentModel.DataAnnotations;

namespace CinemaApi.Helpers
{
    // Limiti di parole che distinguono una recensione (testo vero, da 30 a 1000 parole)
    // da un commento (breve, al massimo 100). Gli stessi numeri sono nel FE (core/constants.ts).
    public static class LimitiParole
    {
        public const int RecensioneMinimo = 30;
        public const int RecensioneMassimo = 1000;
        public const int CommentoMassimo = 100;

        // Parola = sequenza di caratteri senza spazi che contiene almeno una lettera o una cifra:
        // la punteggiatura isolata ("-", "…") e le emoji da sole non contano. Stesso calcolo del FE.
        public static int Conta(string? testo)
        {
            if (string.IsNullOrWhiteSpace(testo)) return 0;
            return testo
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Count(p => p.Any(char.IsLetterOrDigit));
        }
    }

    // [Parole(Minimo = 30, Massimo = 1000)] su una stringa: 400 con un messaggio chiaro se fuori dai limiti.
    // Un testo vuoto lo gestisce [Required]: qui si controlla solo se c'è.
    [AttributeUsage(AttributeTargets.Property)]
    public class ParoleAttribute : ValidationAttribute
    {
        public int Minimo { get; set; }
        public int Massimo { get; set; } = int.MaxValue;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string testo || string.IsNullOrWhiteSpace(testo)) return ValidationResult.Success;
            var parole = LimitiParole.Conta(testo);
            if (parole < Minimo)
                return new ValidationResult($"Servono almeno {Minimo} parole (ora sono {parole})", new[] { validationContext.MemberName! });
            if (parole > Massimo)
                return new ValidationResult($"Al massimo {Massimo} parole (ora sono {parole})", new[] { validationContext.MemberName! });
            return ValidationResult.Success;
        }
    }
}
