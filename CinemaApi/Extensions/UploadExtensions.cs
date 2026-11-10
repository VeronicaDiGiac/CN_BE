using CinemaApi.Helpers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

namespace CinemaApi.Extensions
{
    public static class UploadExtensions
    {
        private const long DimensioneMassima = 5 * 1024 * 1024; // 5 MB

        // Immagini caricate (locandine, avatar): al massimo 5 MB per richiesta
        public static IServiceCollection AddLimitiUpload(this IServiceCollection services)
        {
            services.Configure<IISServerOptions>(o => o.MaxRequestBodySize = DimensioneMassima);
            services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = DimensioneMassima);
            return services;
        }

        // Immagini caricate dagli utenti in una cartella fuori da wwwroot (produzione), servite su /uploads
        public static WebApplication UseUploadEsterni(this WebApplication app)
        {
            if (!PercorsiUpload.Esterna(app.Configuration)) return app;

            var radiceUpload = PercorsiUpload.Radice(app.Configuration, app.Environment);
            Directory.CreateDirectory(radiceUpload);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(radiceUpload),
                RequestPath = "/uploads"
            });
            return app;
        }
    }
}
