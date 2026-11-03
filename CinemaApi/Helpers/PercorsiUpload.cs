namespace CinemaApi.Helpers
{
    // Dove si salvano locandine e avatar caricati dagli utenti. Si servono sempre su /uploads/...
    // In locale: wwwroot/uploads. In produzione conviene una cartella fuori dall'app
    // ("Uploads:Percorso" nelle impostazioni, es. /home/data/uploads su Azure App Service),
    // altrimenti un nuovo deploy cancellerebbe le immagini.
    public static class PercorsiUpload
    {
        public static string Radice(IConfiguration config, IWebHostEnvironment ambiente)
        {
            var configurata = config["Uploads:Percorso"];
            if (string.IsNullOrWhiteSpace(configurata))
                return Path.Combine(ambiente.WebRootPath ?? Path.Combine(ambiente.ContentRootPath, "wwwroot"), "uploads");
            // Un percorso relativo (es. "dati/uploads") parte dalla cartella dell'app
            return Path.IsPathRooted(configurata) ? configurata : Path.Combine(ambiente.ContentRootPath, configurata);
        }

        // true se le immagini stanno fuori da wwwroot (vanno servite a parte)
        public static bool Esterna(IConfiguration config) => !string.IsNullOrWhiteSpace(config["Uploads:Percorso"]);
    }
}
