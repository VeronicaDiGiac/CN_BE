using CinemaApi.Helpers;
using CinemaApi.Services;

namespace CinemaApi.Extensions
{
    public static class ServicesExtensions
    {
        // I service dell'applicazione e i servizi di supporto (errori, worker, Auth0)
        public static IServiceCollection AddCinemaServices(this IServiceCollection services)
        {
            // Scoped: uno per richiesta HTTP, come il CinemaContext che usano
            services.AddScoped<FilmService>();
            services.AddScoped<RecensioniService>();
            services.AddScoped<CommentiService>();
            services.AddScoped<PreferitiService>();
            services.AddScoped<AuthService>();
            services.AddScoped<UtentiService>();
            services.AddScoped<FollowService>();
            services.AddScoped<SegnalazioniService>();
            services.AddScoped<EmailService>();
            services.AddScoped<VisioniService>();
            services.AddScoped<DaVedereService>();
            services.AddScoped<FeedService>();
            services.AddScoped<NotificheService>();
            services.AddScoped<ListeService>();
            services.AddScoped<StatisticheService>();
            services.AddScoped<EpisodiService>();
            services.AddScoped<CalendarioSerieService>();
            services.AddScoped<DiscussioniService>();
            services.AddScoped<AdminService>();
            services.AddScoped<SalvatiService>();

            // Accesso con Google: verifica dei token di Auth0 (singleton per tenere in cache le chiavi pubbliche)
            services.AddSingleton<VerificaAuth0>();

            // Controllo periodico dei nuovi episodi delle serie seguite (notifiche)
            services.AddHostedService<NuoviEpisodiWorker>();

            // Errori imprevisti: gestiti tutti in Helpers/GestoreErrori.cs (niente try/catch nei controller)
            services.AddExceptionHandler<GestoreErrori>();
            services.AddProblemDetails();

            return services;
        }
    }
}
