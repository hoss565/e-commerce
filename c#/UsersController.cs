using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly EcommerceDbContext _context;

        public UsersController(EcommerceDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return users.Select(u => new
            {
                u.UserId,
                u.UserName,
                u.Email,
                u.CreatedAt
            }).ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return new
            {
                user.UserId,
                user.UserName,
                user.Email,
                user.CreatedAt
            };
        }

        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                return BadRequest("Email already registered");
            }

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.UserName == user.UserName))
            {
                return BadRequest("Username already taken");
            }

            // Hash password before saving
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(user.Password));
                user.Password = Convert.ToBase64String(hashedBytes);
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Don't return password in response
            var userResponse = new
            {
                user.UserId,
                user.UserName,
                user.Email,
                user.CreatedAt
            };
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userResponse);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest();
            }

            // If password is being updated, hash it
            if (!string.IsNullOrEmpty(user.Password))
            {
                using (var sha256 = SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(user.Password));
                    user.Password = Convert.ToBase64String(hashedBytes);
                }
            }

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] LoginModel model)
        {
            try
            {
                // Log the incoming request
                Console.WriteLine($"Login attempt for email: {model.Email}");

                // Find user by email
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == model.Email);
                
                if (user == null)
                {
                    Console.WriteLine("User not found");
                    return NotFound("Invalid email or password");
                }

                Console.WriteLine("User found, verifying password");

                // Hash the provided password and compare with stored hash
                using (var sha256 = SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(model.Password));
                    var hashedPassword = Convert.ToBase64String(hashedBytes);

                    Console.WriteLine($"Stored hash: {user.Password}");
                    Console.WriteLine($"Input hash: {hashedPassword}");

                    if (hashedPassword != user.Password)
                    {
                        Console.WriteLine("Password mismatch");
                        return NotFound("Invalid email or password");
                    }
                }

                Console.WriteLine("Password verified, generating response");

                // Create user response without sensitive data
                var userResponse = new
                {
                    user.UserId,
                    user.UserName,
                    user.Email,
                    user.CreatedAt,
                    Token = GenerateToken(user)
                };

                Console.WriteLine("Login successful");
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Login error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred during login. Please try again.");
            }
        }

        // Helper method to create a test user
        [HttpPost("createTestUser")]
        public async Task<ActionResult<User>> CreateTestUser()
        {
            try
            {
                var testUser = new User
                {
                    UserName = "test",
                    Email = "test@test.com",
                    Password = "test123" // This will be hashed
                };

                // Hash password
                using (var sha256 = SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(testUser.Password));
                    testUser.Password = Convert.ToBase64String(hashedBytes);
                }

                _context.Users.Add(testUser);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Test user created successfully", Email = testUser.Email, Password = "test123" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating test user: {ex.Message}");
                return StatusCode(500, "Error creating test user");
            }
        }

        private string GenerateToken(User user)
        {
            // Simple token generation - in production, use proper JWT tokens
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.UserId}:{user.Email}:{DateTime.UtcNow.Ticks}"));
        }
    }

    public class LoginModel
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
