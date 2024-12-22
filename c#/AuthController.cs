using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EcommerceApi.Models;
using EcommerceApi.Models.DTOs;
using EcommerceApi.Helpers;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly EcommerceDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(EcommerceDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            _logger.LogInformation($"Registration attempt for email: {registerDto.Email}");
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                {
                    return BadRequest("Email already exists");
                }

                // Create new user
                var user = new User
                {
                    UserName = registerDto.UserName.Trim(), // Combined first and last name from frontend
                    Email = registerDto.Email,
                    Password = PasswordHasher.HashPassword(registerDto.Password),
                    CreatedAt = DateTime.UtcNow
                };

                // Create user
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"User registered successfully with email: {user.Email}");

                // Create cart for the new user
                var cart = new Cart
                {
                    UserId = user.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Created cart for user: {user.Email}");

                // Generate JWT token
                var token = GenerateJwtToken(user);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    User = new UserDto
                    {
                        UserId = user.UserId,
                        UserName = user.UserName,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, "An error occurred during registration");
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Login attempt with email: {loginDto.Email}");
                
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                if (user == null)
                {
                    _logger.LogWarning($"User not found with email: {loginDto.Email}");
                    return Unauthorized("Invalid email or password");
                }

                _logger.LogInformation($"Found user: {user.Email}, verifying password");
                var isPasswordValid = PasswordHasher.VerifyPassword(user.Password, loginDto.Password);
                _logger.LogInformation($"Password verification result: {isPasswordValid}");

                if (!isPasswordValid)
                {
                    return Unauthorized("Invalid email or password");
                }

                // Generate JWT token
                var token = GenerateJwtToken(user);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    User = new UserDto
                    {
                        UserId = user.UserId,
                        UserName = user.UserName,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, "An error occurred during login");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured")));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("userName", user.UserName)
            };

            // Get token duration from configuration or default to 7 days
            var durationInDays = int.TryParse(_configuration["Jwt:DurationInDays"], out int days) ? days : 7;
            
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(durationInDays),
                signingCredentials: credentials
            );

            _logger.LogInformation($"Generated JWT token for user {user.Email} with duration {durationInDays} days");

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RegisterDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
