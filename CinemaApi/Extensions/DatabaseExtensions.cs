using CinemaApi.Models;
using CinemaApi.Services;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Extensions
{
    public static class DatabaseExtensions
    {
        // DbContext su SQL Server (stringa di connessione "CinemaDB")
        public static IServiceCollection AddCinemaDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<CinemaContext>(opt =>
                opt.UseSqlServer(config.GetConnectionString("CinemaDB")));
            return services;
        }

        // Film salvati prima della colonna TitoloChiave (Database/FilmTitoloChiave.sql): la si calcola
        // una volta sola. Se è già tutto a posto è una sola query, quindi resta sicuro a ogni avvio.
        public static async Task CompletaChiaviTitoloAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<FilmService>().CompletaChiaviTitolo();
        }
    }
}
