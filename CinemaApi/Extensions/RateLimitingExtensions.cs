using CinemaApi.Controllers;
using System.Threading.RateLimiting;

namespace CinemaApi.Extensions
{
    public static class RateLimitingExtensions
    {
        // Limiti di richieste per indirizzo IP (finestra fissa). Le policy si applicano ai controller
        // con [EnableRateLimiting(...)] usando i nomi definiti in AuthController e TmdbController.
        public static IServiceCollection AddCinemaRateLimiting(this IServiceCollection services, IConfiguration config)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // "Password dimenticata?": massimo 10 richieste ogni 15 minuti per indirizzo IP,
                // così nessuno può usare l'endpoint per riempire di mail la casella di qualcun altro
                options.AddPolicy(AuthController.LimitePasswordDimenticata, httpContext =>
                    FinestraPerIp(httpContext, 10, TimeSpan.FromMinutes(15)));

                // Login, registrazione e accesso con Google: 10 tentativi al minuto per IP rendono
                // inutile provare password a raffica, senza disturbare chi sbaglia una volta
                // (configurabile con "Limiti:AccessiAlMinuto": i test automatici in locale ne fanno molti di più)
                var accessiAlMinuto = config.GetValue("Limiti:AccessiAlMinuto", 10);
                options.AddPolicy(AuthController.LimiteAccesso, httpContext =>
                    FinestraPerIp(httpContext, accessiAlMinuto, TimeSpan.FromMinutes(1)));

                // TMDB: una visita al catalogo fa già circa 10 richieste (sezioni, ricerca, persone, filtri), quindi
                // 150 al minuto per IP lasciano navigare senza blocchi. Le risposte sono comunque in cache nel BE.
                // Configurabile con "Limiti:TmdbAlMinuto" (i test end-to-end in locale ne fanno di più)
                var tmdbAlMinuto = config.GetValue("Limiti:TmdbAlMinuto", 150);
                options.AddPolicy(TmdbController.LimiteTmdb, httpContext =>
                    FinestraPerIp(httpContext, tmdbAlMinuto, TimeSpan.FromMinutes(1)));
            });
            return services;
        }

        // Un contatore per ogni indirizzo IP: al massimo "permessi" richieste in ogni "finestra"
        private static RateLimitPartition<string> FinestraPerIp(HttpContext httpContext, int permessi, TimeSpan finestra) =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "sconosciuto",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permessi,
                    Window = finestra
                });
    }
}
