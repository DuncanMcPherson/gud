using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using gud.Server.DTO;
using gud.Server.Models;
using gud.Server.Repositories.Interfaces;
using gud.Server.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace gud.Server.Services.Implementations;

public class AuthService(IUserRepository userRepository, IConfiguration config) : IAuthService
{
    public async Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return (false, "Username and password are required", null);

        if (await userRepository.UsernameExistsAsync(request.Username))
            return (false, "Username already taken", null);

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await userRepository.CreateAsync(user);

        var token = IssueToken(user.Id, user.Username);
        return (true, null, new AuthResponse(user.Username, token));
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return (false, "Username and password are required", null);

        var user = await userRepository.GetByUsernameAsync(request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return (false, "Invalid username or password", null);
        var token = IssueToken(user.Id, user.Username);
        return (true, null, new AuthResponse(user.Username, token));
    }

    public string IssueToken(int userId, string username)
    {
        var secret = config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiryMinutes = int.Parse(config["Jwt:ExpiryMinutes"] ?? "10080");
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}