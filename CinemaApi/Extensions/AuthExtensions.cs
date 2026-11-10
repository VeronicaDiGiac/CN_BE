using CinemaApi.Models;
using CinemaApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace CinemaApi.Extensions
{
    public static class AuthExtensions
    {
        // Autenticazione con i JWT emessi da AuthService (anche dopo l'accesso con Google tramite Auth0:
        // il BE verifica il token di Auth0 in VerificaAuth0 e poi emette il proprio JWT)
        public static IServiceCollection AddCinemaAuth(this IServiceCollection services, IConfiguration config)
        {
            // La chiave JWT non sta in appsettings.json: va negli User Secrets
            // (dotnet user-secrets set "Jwt:Key" "<chiave di almeno 32 caratteri>").
            var jwtKey = config["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "Jwt:Key mancante. Impostala con: dotnet user-secrets set \"Jwt:Key\" \"<chiave di almeno 32 caratteri>\"");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = config["Jwt:Issuer"],
                        ValidAudience = config["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };

                    // Dopo un cambio o un reset della password i token emessi prima non valgono più
                    // (si scollegano tutti i dispositivi). Costa una query leggera per ogni richiesta autenticata.
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var idUtente = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                            var versione = context.Principal?.FindFirst(AuthService.ClaimVersionePassword)?.Value;
                            var db = context.HttpContext.RequestServices.GetRequiredService<CinemaContext>();

                            if (!int.TryParse(idUtente, out var id) || !await AuthService.TokenAncoraValido(db, id, versione))
                                context.Fail("Sessione non più valida: la password è stata cambiata");
                        }
                    };
                });
            return services;
        }
    }
}
