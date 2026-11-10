using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace CinemaApi.Extensions
{
    public static class CompressionExtensions
    {
        // Risposte compresse (brotli/gzip): il JSON pesa 4-5 volte meno, si nota soprattutto su telefono
        public static IServiceCollection AddCinemaCompression(this IServiceCollection services)
        {
            services.AddResponseCompression(o =>
            {
                o.EnableForHttps = true;
                o.Providers.Add<BrotliCompressionProvider>();
                o.Providers.Add<GzipCompressionProvider>();
            });
            services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
            return services;
        }

        // Compressione per tutto tranne l'accesso (/api/auth): le risposte con il token non si comprimono,
        // per prudenza contro gli attacchi che sfruttano la compressione (BREACH)
        public static WebApplication UseCinemaCompression(this WebApplication app)
        {
            app.UseWhen(c => !c.Request.Path.StartsWithSegments("/api/auth"), ramo => ramo.UseResponseCompression());
            return app;
        }
    }
}
