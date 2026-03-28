using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixteenSounds.Data;
using SixteenSounds.DTO;
using SixteenSounds.Models;
using System.Security.Claims;
using static SixteenSounds.Controllers.AuthController;

namespace SixteenSounds.Controllers
{
    [Authorize] // Domyślnie wszystko wymaga logowania
    [ApiController]
    [Route("api/[controller]")]
    public class SampleController : ControllerBase
    {
        private readonly SixteenSoundsDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SampleController(SixteenSoundsDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [AllowAnonymous] // Każdy może przeglądać listę
        [HttpGet]
        public async Task<IActionResult> GetSamples()
        {
            var samples = await _context.Samples.ToListAsync();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var result = samples.Select(s => new {
                s.Id,
                s.Name,
                s.Category,
                FileUrl = $"{baseUrl}/samples/{Path.GetFileName(s.FileName)}"
            });

            return Ok(result);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadSample([FromForm] SampleDto dto, IFormFile file)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int currentUserId = int.Parse(userIdClaim.Value);

            if (file == null || file.Length == 0)
                return BadRequest("Plik jest pusty lub nie został przesłany.");

            var folderPath = Path.Combine(_env.WebRootPath, "samples");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var sample = new Sample
            {
                Name = dto.Name,
                Category = "General",
                FileName = filePath, // Upewnij się, że w Sample.cs masz FileName
                CreatedAt = DateTime.UtcNow,
                UserId = currentUserId
            };

            _context.Samples.Add(sample);
            await _context.SaveChangesAsync();

            return Ok("Wgrano pomyślnie!");
        }

        [AllowAnonymous] // KLUCZOWA ZMIANA: Pozwalamy niezalogowanym zresetować hasło
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return BadRequest("Użytkownik o podanym adresie nie istnieje.");
            }

            // Hashujemy nowe hasło
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Hasło zostało pomyślnie zmienione. Możesz się zalogować.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSample(int id)
        {
            var sample = await _context.Samples.FindAsync(id);
            if (sample == null) return NotFound();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int currentUserId = int.Parse(userIdClaim.Value);

            if (sample.UserId != currentUserId)
                return Forbid();

            if (System.IO.File.Exists(sample.FileName))
                System.IO.File.Delete(sample.FileName);

            _context.Samples.Remove(sample);
            await _context.SaveChangesAsync();

            return Ok("Usunięto pomyślnie.");
        }
    }
}