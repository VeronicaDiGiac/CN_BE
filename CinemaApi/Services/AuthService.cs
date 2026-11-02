using CinemaApi.Helpers;
using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.WebUtilities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CinemaApi.Services
{
    public class AuthService
    {
        // Durata del link "Password dimenticata?"
        public const int MinutiValiditaReset = 30;

        // Claim del JWT con la "versione" della password (ticks dell'ultimo cambio)
        public const string ClaimVersionePassword = "versionePassword";

        private readonly CinemaContext _db;
        private readonly IConfiguration _config;
        private readonly EmailService _email;
        private readonly ILogger<AuthService> _logger;

        public AuthService(CinemaContext db, IConfiguration config, EmailService email, ILogger<AuthService> logger)
        {
            _db = db;
            _config = config;
            _email = email;
            _logger = logger;
        }

        // Ritorna il LoginResponseDto se le credenziali sono corrette, altrimenti null
        public async Task<LoginResponseDto?> Login(LoginDto dto)
        {
            var utente = await _db.Utenti.FirstOrDefaultAsync(u => u.Email == dto.Email);
            // Senza hash (utente inserito a mano nel DB) BCrypt.Verify lancerebbe un'eccezione
            if (utente == null || string.IsNullOrEmpty(utente.PasswordHash)) return null;

            bool passwordCorretta = BCrypt.Net.BCrypt.Verify(dto.Password, utente.PasswordHash);
            if (!passwordCorretta) return null;

            var tokenString = GeneraToken(utente);

            return new LoginResponseDto
            {
                IdUtente = utente.IdUtente,
                UserName = utente.UserName,
                Ruolo = utente.Ruolo,
                Token = tokenString
            };
        }

        // Ritorna null se l'email esiste già, altrimenti il LoginResponseDto del nuovo utente
        public async Task<LoginResponseDto?> Register(RegisterDto dto)
        {
            var utenteEsistente = await _db.Utenti.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (utenteEsistente != null) return null;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var utente = new Utente
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Ruolo = Ruoli.User,
                DataDiRegistrazione = DateTime.Now
            };
            _db.Utenti.Add(utente);
            await _db.SaveChangesAsync();

            var tokenString = GeneraToken(utente);

            return new LoginResponseDto
            {
                IdUtente = utente.IdUtente,
                UserName = utente.UserName,
                Ruolo = utente.Ruolo,
                Token = tokenString
            };
        }

        // Ritorna null se l'utente non esiste, false se la password attuale è sbagliata,
        // true se la password è stata cambiata.
        public async Task<bool?> CambiaPassword(int idUtente, CambiaPasswordDto dto)
        {
            var utente = await _db.Utenti.FindAsync(idUtente);
            if (utente == null) return null;

            bool passwordCorretta = BCrypt.Net.BCrypt.Verify(dto.PasswordAttuale, utente.PasswordHash);
            if (!passwordCorretta) return false;

            utente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuovaPassword);
            // Scollega tutti i dispositivi: i token emessi finora non valgono più
            utente.DataCambioPassword = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await InviaConfermaPasswordCambiata(utente);
            return true;
        }

        // Nuovo token per chi ha appena cambiato la password dal profilo,
        // così resta collegato solo il dispositivo da cui ha fatto il cambio
        public async Task<LoginResponseDto?> CreaSessione(int idUtente)
        {
            var utente = await _db.Utenti.FindAsync(idUtente);
            if (utente == null) return null;

            return new LoginResponseDto
            {
                IdUtente = utente.IdUtente,
                UserName = utente.UserName,
                Ruolo = utente.Ruolo,
                Token = GeneraToken(utente)
            };
        }

        // Usato dalla validazione del JWT (Program.cs): il token vale solo se è stato
        // emesso dopo l'ultimo cambio password dell'utente
        public static async Task<bool> TokenAncoraValido(CinemaContext db, int idUtente, string? versionePassword)
        {
            var dataCambio = await db.Utenti
                .Where(u => u.IdUtente == idUtente)
                .Select(u => new { u.DataCambioPassword })
                .FirstOrDefaultAsync();
            if (dataCambio == null) return false; // utente eliminato

            return VersionePassword(dataCambio.DataCambioPassword) == (versionePassword ?? VersionePassword(null));
        }

        // I token emessi prima di questa funzione non hanno il claim: valgono finché l'utente non cambia password
        private static string VersionePassword(DateTime? dataCambio) => (dataCambio?.Ticks ?? 0).ToString();

        // Crea un link di reimpostazione e lo manda per email.
        // Non dice mai se l'email esiste: il controller risponde sempre allo stesso modo.
        public async Task PasswordDimenticata(PasswordDimenticataDto dto)
        {
            var utente = await _db.Utenti.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (utente == null) return;

            var frontendUrl = _config["FrontendUrl"]
                ?? throw new InvalidOperationException("FrontendUrl mancante nella configurazione");

            // Vale solo l'ultimo link richiesto
            await AnnullaLinkAttivi(utente.IdUtente);

            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            _db.PasswordReset.Add(new PasswordReset
            {
                IdUtente = utente.IdUtente,
                TokenHash = HashToken(token),
                DataCreazione = DateTime.UtcNow,
                Scadenza = DateTime.UtcNow.AddMinutes(MinutiValiditaReset)
            });
            await _db.SaveChangesAsync();

            var link = $"{frontendUrl.TrimEnd('/')}/reimposta-password?token={token}";

            try
            {
                await _email.InviaLinkReimpostaPassword(utente.Email, utente.UserName, link, MinutiValiditaReset);
            }
            catch (Exception ex)
            {
                // Un errore qui non deve cambiare la risposta, altrimenti si capirebbe che l'email esiste
                _logger.LogError(ex, "Invio email di reimpostazione password non riuscito (utente {IdUtente})", utente.IdUtente);
            }
        }

        // Ritorna false se il token non esiste, è scaduto o è già stato usato
        public async Task<bool> ReimpostaPassword(ReimpostaPasswordDto dto)
        {
            var tokenHash = HashToken(dto.Token);
            var richiesta = await _db.PasswordReset
                .Include(p => p.Utente)
                .FirstOrDefaultAsync(p => p.TokenHash == tokenHash && !p.Usato && p.Scadenza > DateTime.UtcNow);
            if (richiesta?.Utente == null) return false;

            // Segna il link come usato con una sola UPDATE condizionata:
            // se arrivano due richieste insieme con lo stesso link, ne passa una sola
            var prenotato = await _db.PasswordReset
                .Where(p => p.IdPasswordReset == richiesta.IdPasswordReset && !p.Usato)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Usato, true));
            if (prenotato == 0) return false;

            richiesta.Utente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuovaPassword);
            // Scollega tutti i dispositivi: qualcuno potrebbe essere entrato con la vecchia password
            richiesta.Utente.DataCambioPassword = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await AnnullaLinkAttivi(richiesta.IdUtente);

            await InviaConfermaPasswordCambiata(richiesta.Utente);
            return true;
        }

        private Task AnnullaLinkAttivi(int idUtente) =>
            _db.PasswordReset
                .Where(p => p.IdUtente == idUtente && !p.Usato)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Usato, true));

        // SHA-256 basta: il token è casuale a 256 bit, non serve un hash lento come BCrypt
        private static string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

        private async Task InviaConfermaPasswordCambiata(Utente utente)
        {
            try
            {
                await _email.InviaConfermaPasswordCambiata(utente.Email, utente.UserName);
            }
            catch (Exception ex)
            {
                // La password è già cambiata: un problema con la mail non deve far fallire la richiesta
                _logger.LogError(ex, "Invio email di conferma cambio password non riuscito (utente {IdUtente})", utente.IdUtente);
            }
        }

        // Metodo privato: solo questo service sa come si genera un token
        private string GeneraToken(Utente utente)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, utente.IdUtente.ToString()),
                new Claim(ClaimTypes.Name, utente.UserName),
                new Claim(ClaimTypes.Role, utente.Ruolo),
                new Claim(ClaimVersionePassword, VersionePassword(utente.DataCambioPassword))
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}