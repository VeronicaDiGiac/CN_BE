using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaApi.Services
{
    // Verifica gli ID token emessi da Auth0 (accesso con Google).
    // Le chiavi pubbliche si scaricano dal tenant (.well-known/openid-configuration) e restano in cache:
    // per questo è un singleton. Domain e ClientId stanno in appsettings.json (sezione "Auth0"):
    // sono valori pubblici, nessun segreto.
    public class VerificaAuth0
    {
        // Il token si scambia appena tornati da Auth0: uno emesso da più di così non si accetta,
        // così un token intercettato non resta utilizzabile per tutta la sua durata (ore)
        private static readonly TimeSpan EtaMassimaToken = TimeSpan.FromMinutes(10);

        private readonly string? _issuer;
        private readonly string? _clientId;
        private readonly ConfigurationManager<OpenIdConnectConfiguration>? _configurazione;

        public VerificaAuth0(IConfiguration config)
        {
            var dominio = config["Auth0:Domain"];
            _clientId = config["Auth0:ClientId"];
            if (string.IsNullOrWhiteSpace(dominio) || string.IsNullOrWhiteSpace(_clientId)) return;

            _issuer = $"https://{dominio}/";
            _configurazione = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{_issuer}.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = true });
        }

        public bool Configurato => _configurazione != null;

        // Le informazioni dell'utente se il token è valido (firma, emittente, destinatario, scadenza,
        // emesso da poco), altrimenti null
        public async Task<UtenteAuth0?> Verifica(string idToken)
        {
            if (_configurazione == null) return null;
            var openId = await _configurazione.GetConfigurationAsync(CancellationToken.None);

            var parametri = new TokenValidationParameters
            {
                ValidIssuer = _issuer,
                ValidAudience = _clientId,
                IssuerSigningKeys = openId.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
            // Senza la mappatura i claim restano con i nomi originali (email, name, sub…)
            var gestore = new JwtSecurityTokenHandler { MapInboundClaims = false };

            ClaimsPrincipal principale;
            SecurityToken token;
            try
            {
                principale = gestore.ValidateToken(idToken, parametri, out token);
            }
            catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
            {
                return null;
            }

            if (DateTime.UtcNow - token.ValidFrom > EtaMassimaToken + parametri.ClockSkew) return null;

            string? Claim(string nome) => principale.FindFirst(nome)?.Value;
            return new UtenteAuth0(
                Claim("sub") ?? "",
                Claim("email"),
                string.Equals(Claim("email_verified"), "true", StringComparison.OrdinalIgnoreCase),
                Claim("name") ?? Claim("nickname"));
        }
    }

    public record UtenteAuth0(string Id, string? Email, bool EmailVerificata, string? Nome);
}
