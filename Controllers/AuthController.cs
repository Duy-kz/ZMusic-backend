using Microsoft.AspNetCore.Mvc;
using zmusic_backend.Application.DTOs.Auth;
using zmusic_backend.Application.Interfaces;

namespace zmusic_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", request?.Email ?? "null");
                
                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    
                    _logger.LogWarning("Login validation failed: {Errors}", string.Join(", ", errors));
                    return BadRequest(new { message = "Invalid input data", errors = errors });
                }

                var response = await _authService.LoginAsync(request);
                _logger.LogInformation("Login successful for email: {Email}", request.Email);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Login failed for email {Email}: {Message}", request?.Email, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for email {Email}", request?.Email);
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest? request)
        {
            try
            {
                _logger.LogInformation("=== REGISTER REQUEST START ===");
                
                // Check if request is null
                if (request == null)
                {
                    _logger.LogWarning("Register request is null");
                    return BadRequest(new { 
                        message = "Request body is required",
                        error = "NullRequest"
                    });
                }

                _logger.LogInformation("Request received - Username: '{Username}', Email: '{Email}', Password Length: {PasswordLength}", 
                    request.Username ?? "null", request.Email ?? "null", request.Password?.Length ?? 0);

                // Manual validation first (bypass ModelState issues)
                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.Username))
                    validationErrors.Add("Username is required");
                else if (request.Username.Length < 3)
                    validationErrors.Add("Username must be at least 3 characters long");
                else if (request.Username.Length > 100)
                    validationErrors.Add("Username cannot exceed 100 characters");

                if (string.IsNullOrWhiteSpace(request.Email))
                    validationErrors.Add("Email is required");
                else if (!IsValidEmail(request.Email))
                    validationErrors.Add("Email format is invalid");

                if (string.IsNullOrWhiteSpace(request.Password))
                    validationErrors.Add("Password is required");
                else if (request.Password.Length < 6)
                    validationErrors.Add("Password must be at least 6 characters long");

                if (validationErrors.Any())
                {
                    _logger.LogWarning("Manual validation failed: {Errors}", string.Join(", ", validationErrors));
                    return BadRequest(new { 
                        message = "Validation failed", 
                        errors = validationErrors
                    });
                }

                // Check ModelState as well
                if (!ModelState.IsValid)
                {
                    var modelErrors = ModelState
                        .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    
                    _logger.LogWarning("ModelState validation failed: {@ModelErrors}", modelErrors);
                }

                _logger.LogInformation("Validation passed, calling AuthService...");
                
                var response = await _authService.RegisterAsync(request);
                
                _logger.LogInformation("Registration successful for email: {Email}", request.Email);
                _logger.LogInformation("=== REGISTER REQUEST END ===");
                
                return CreatedAtAction(nameof(Register), response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Registration business logic error for {Email}: {Message}", request?.Email, ex.Message);
                return BadRequest(new { message = ex.Message, error = "BusinessLogicError" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for email {Email}", request?.Email);
                return StatusCode(500, new { 
                    message = "An unexpected error occurred during registration", 
                    error = "InternalServerError",
                    details = ex.Message 
                });
            }
        }

        // Helper method for email validation
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Test endpoint with simple registration (no validation annotations)
        [HttpPost("register-simple")]
        public async Task<ActionResult<AuthResponse>> RegisterSimple()
        {
            try
            {
                _logger.LogInformation("=== SIMPLE REGISTER TEST ===");
                
                // Create a test registration request
                var request = new RegisterRequest
                {
                    Username = "testuser" + DateTime.Now.Ticks,
                    Email = $"testuser{DateTime.Now.Ticks}@example.com",
                    Password = "password123"
                };

                _logger.LogInformation("Creating test user: {Username}, {Email}", request.Username, request.Email);
                
                var response = await _authService.RegisterAsync(request);
                
                _logger.LogInformation("Test registration successful");
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test registration failed");
                return StatusCode(500, new { 
                    message = "Test registration failed", 
                    error = ex.Message 
                });
            }
        }

        // Test endpoint to check if email exists
        [HttpGet("check-email/{email}")]
        public async Task<ActionResult> CheckEmail(string email)
        {
            try
            {
                _logger.LogInformation("Checking email existence: {Email}", email);
                var exists = await _authService.UserExistsAsync(email);
                _logger.LogInformation("Email {Email} exists: {Exists}", email, exists);
                return Ok(new { email = email, exists = exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email: {Email}", email);
                return StatusCode(500, new { message = "Error checking email" });
            }
        }

        // Debug endpoint to test basic functionality
        [HttpGet("debug/test")]
        public ActionResult DebugTest()
        {
            _logger.LogInformation("Debug test endpoint called");
            return Ok(new { 
                message = "AuthController is working",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            });
        }

        // Debug endpoint to test database connection
        [HttpGet("debug/db-test")]
        public async Task<ActionResult> DatabaseTest([FromServices] IAuthService authService)
        {
            try
            {
                _logger.LogInformation("Testing database connection...");
                var testResult = await authService.UserExistsAsync("test@nonexistent.com");
                _logger.LogInformation("Database test successful");
                return Ok(new { 
                    message = "Database connection working",
                    testResult = testResult,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database test failed");
                return StatusCode(500, new { 
                    message = "Database test failed", 
                    error = ex.Message 
                });
            }
        }
    }
}