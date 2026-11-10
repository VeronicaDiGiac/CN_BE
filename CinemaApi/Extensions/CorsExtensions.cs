namespace CinemaApi.Extensions
{
    public static class CorsExtensions
    {
        public static IServiceCollection AddCinemaCors(this IServiceCollection services)
        {
            services.AddCors();
            return services;
        }

        // Solo il FE può chiamare l'API dal browser (gli altri siti vengono bloccati dal browser).
        // Origini in configurazione: "Cors:Origini" (online: Cors__Origini__0 = indirizzo del sito)
        public static WebApplication UseCinemaCors(this WebApplication app)
        {
            var originiFe = app.Configuration.GetSection("Cors:Origini").Get<string[]>() ?? Array.Empty<string>();
            app.UseCors(p => p.WithOrigins(originiFe).AllowAnyHeader().AllowAnyMethod());
            return app;
        }
    }
}
