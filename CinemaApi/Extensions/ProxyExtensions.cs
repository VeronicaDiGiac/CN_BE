using Microsoft.AspNetCore.HttpOverrides;

namespace CinemaApi.Extensions
{
    public static class ProxyExtensions
    {
        // In produzione l'API sta dietro un proxy: l'IP del visitatore arriva in X-Forwarded-For.
        // Senza questo i limiti per IP vedrebbero solo l'indirizzo del proxy. ForwardLimit = 1: si usa
        // solo il valore aggiunto dal proxy, non quelli che un client potrebbe inventare.
        public static IServiceCollection AddProxyHeaders(this IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                o.ForwardLimit = 1;
                o.KnownNetworks.Clear();
                o.KnownProxies.Clear();
            });
            return services;
        }

        // Fuori dallo sviluppo: IP vero del visitatore e HSTS (il browser userà sempre HTTPS per questo sito)
        public static WebApplication UseProxyEHsts(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseForwardedHeaders();
                app.UseHsts();
            }
            return app;
        }
    }
}
