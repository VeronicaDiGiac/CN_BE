using CinemaApi.Services;

namespace CinemaApi.Extensions
{
    public static class TmdbExtensions
    {
        // TMDB: HttpClient dedicato + cache in memoria per non ripetere le stesse chiamate
        public static IServiceCollection AddTmdb(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddHttpClient<TmdbService>(c =>
            {
                c.BaseAddress = new Uri("https://api.themoviedb.org/3/");
                c.Timeout = TimeSpan.FromSeconds(10);
            })
            // TMDB manda le risposte compresse: meno dati da scaricare
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Brotli
            });

            // Wikidata e Wikipedia (curiosità sulla produzione): Wikimedia chiede uno User-Agent che
            // identifichi l'applicazione, altrimenti può rifiutare le richieste
            services.AddHttpClient<WikipediaService>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(10);
                c.DefaultRequestHeaders.UserAgent.ParseAdd("CineverseBot/1.0 (https://cineverse-beta.netlify.app)");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Brotli
            });
            return services;
        }
    }
}
