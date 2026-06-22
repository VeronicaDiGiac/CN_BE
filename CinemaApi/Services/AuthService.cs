using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CinemaApi.Services
{
    public class AuthService
    {
        private readonly CinemaContext _db;
        private readonly IConfiguration _config;

        public AuthService(CinemaContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // Ritorna il LoginResponseDto se le credenziali sono corrette, altrimenti null
        public async Task<LoginResponseDto?> Login(LoginDto dto)
        {
            var utente = await _db.Utenti.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (utente == null) return null;

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
                Ruolo = "User",
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

        // Metodo privato: solo questo service sa come si genera un token
        private string GeneraToken(Utente utente)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, utente.IdUtente.ToString()),
                new Claim(ClaimTypes.Name, utente.UserName),
                new Claim(ClaimTypes.Role, utente.Ruolo)
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