using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixteenSounds.Data;
using SixteenSounds.Models;

namespace SixteenSounds.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SixteenSoundsDbContext _context;

        public AuthController(SixteenSoundsDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
        {
            // 1. Sprawdzamy czy istnieje (AnyAsync - poprawiona nazwa)
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Użytkownik o podanym emailu już istnieje.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            // 2. Dodajemy i KONIECZNIE zapisujemy zmiany
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Konto stworzone pomyślnie.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            // Szukamy użytkownika po mailu
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return BadRequest("Użytkownik o podanym adresie nie istnieje.");
            }

            // Hashujemy nowe hasło biblioteką BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Hasło zostało pomyślnie zmienione.");
        }

        // Ta klasa musi być w tym samym pliku na dole lub w folderze DTO
        public class ResetPasswordDto
        {
            public string Email { get; set; }
            public string NewPassword { get; set; }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return BadRequest("Nie znaleziono użytkownika.");
            }

            // 3. Weryfikacja (poprawione nawiasy)
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Niepoprawne hasło.");
            }

            return Ok($"Witaj {user.Username}! Logowanie udane!");
        }
    }

    // DTOs zostają bez zmian, są poprawne
    public class UserRegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}