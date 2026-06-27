using Azure.Storage.Blobs;
using BudgetApp.Data;
using BudgetApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BudgetApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly BudgetContext _context;
        private readonly IConfiguration _configuration;

        public UserController(BudgetContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register(User user)
        {
            
            if (_context.Users.Any(u => u.Email == user.Email))
                return BadRequest("Użytkownik z takim emailem już istnieje.");

            
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(user.PasswordHash);
                var hash = sha.ComputeHash(bytes);
                user.PasswordHash = Convert.ToBase64String(hash);
            }

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("Rejestracja zakończona sukcesem.");
        }


        [HttpPost("login")]
        public IActionResult Login(User loginData)
        {
            
            string hashedPassword;
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(loginData.PasswordHash);
                var hash = sha.ComputeHash(bytes);
                hashedPassword = Convert.ToBase64String(hash);
            }

            
            var user = _context.Users
                .FirstOrDefault(u => u.Email == loginData.Email && u.PasswordHash == hashedPassword);

            if (user == null)
                return Unauthorized("Niepoprawny email lub hasło.");

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName
            });
        }


        [HttpPost("{id}/photo")]
        public async Task<IActionResult> UploadPhoto(int id, IFormFile file)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
                return NotFound("Użytkownik nie istnieje.");

            if (file == null || file.Length == 0)
                return BadRequest("Nie przesłano pliku.");

            var connectionString = _configuration["AzureStorage:ConnectionString"];
            var containerName = _configuration["AzureStorage:ContainerName"];

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var blobClient = containerClient.GetBlobClient(fileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            user.PhotoUrl = blobClient.Uri.ToString();
            _context.SaveChanges();

            return Ok(new { user.PhotoUrl });
        }
    }
}
