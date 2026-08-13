using gud.Server.DTO;

namespace gud.Server.Services.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request);
    string IssueToken(int userId, string username);
    
    Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request);
}