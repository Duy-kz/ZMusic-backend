using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using zmusic_backend.Application.DTOs.Auth;
using zmusic_backend.Application.Interfaces;
using zmusic_backend.Domain.Entities;
using zmusic_backend.Infrastructure.Data;

namespace zmusic_backend.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ZMusicDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ZMusicDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(
                _configuration.GetValue<int>("JwtSettings:ExpiryHours"));

            return new AuthResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                ExpiresAt = expiresAt
            };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                if (await UserExistsAsync(request.Email))
                {
                    throw new InvalidOperationException("User with this email already exists");
                }

                // Check if username already exists
                var existingUsername = await _context.Users
                    .AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());
                
                if (existingUsername)
                {
                    throw new InvalidOperationException("Username is already taken");
                }

                _logger.LogInformation("Creating new user with email: {Email}", request.Email);

                var user = new User
                {
                    Username = request.Username.Trim(),
                    Email = request.Email.Trim().ToLowerInvariant(),
                    PasswordHash = HashPassword(request.Password),
                    Role = "User",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);

                var token = GenerateJwtToken(user);
                var expiresAt = DateTime.UtcNow.AddHours(
                    _configuration.GetValue<int>("JwtSettings:ExpiryHours"));

                return new AuthResponse
                {
                    Token = token,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    ExpiresAt = expiresAt
                };
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw business logic exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
                throw new InvalidOperationException("An error occurred during registration. Please try again.");
            }
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        private string HashPassword(string password)
        {
            try
            {
                return BCrypt.Net.BCrypt.HashPassword(password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hashing password");
                throw new InvalidOperationException("Error processing password");
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }

        private string GenerateJwtToken(User user)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"];
                var issuer = jwtSettings["Issuer"];
                var audience = jwtSettings["Audience"];
                var expiryHours = jwtSettings.GetValue<int>("JwtSettings:ExpiryHours", 24);

                if (string.IsNullOrEmpty(secretKey))
                {
                    throw new InvalidOperationException("JWT SecretKey is not configured");
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(expiryHours),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user: {UserId}", user.Id);
                throw new InvalidOperationException("Error generating authentication token");
            }
        }
    }
}