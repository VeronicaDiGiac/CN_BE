namespace CinemaApi.Helpers
{
    public static class FileValidationHelper
    {
        private static readonly string[] EstensioniPermesse = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long DimensioneMassimaBytes = 5 * 1024 * 1024; // 5 MB

        public static bool EImmagineValida(IFormFile file, out string errore)
        {
            errore = string.Empty;

            if (file == null || file.Length == 0)
            {
                errore = "File non fornito";
                return false;
            }

            if (file.Length > DimensioneMassimaBytes)
            {
                errore = "Il file supera la dimensione massima di 5 MB";
                return false;
            }

            var estensione = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!EstensioniPermesse.Contains(estensione))
            {
                errore = "Estensione non permessa. Usa jpg, jpeg, png o webp";
                return false;
            }

            return true;
        }
    

    public static async Task<bool> HaFirmaBinariaValida(IFormFile file)
        {
            var firmeValide = new Dictionary<string, byte[]>
    {
        { ".jpg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47 } }
    };

            var estensione = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!firmeValide.ContainsKey(estensione)) return false;

            var firmaAttesa = firmeValide[estensione];
            var bytesIniziali = new byte[firmaAttesa.Length];

            using var stream = file.OpenReadStream();
            await stream.ReadAsync(bytesIniziali, 0, firmaAttesa.Length);

            return bytesIniziali.SequenceEqual(firmaAttesa);
        }
    }
}