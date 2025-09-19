using zmusic_backend.Application.DTOs.Auth;

namespace zmusic_backend.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<bool> UserExistsAsync(string email);
    }
}