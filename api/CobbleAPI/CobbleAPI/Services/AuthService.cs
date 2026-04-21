using CobbleAPI.Interfaces;
using CobbleAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CobbleAPI.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));

        // Define the claims for the JWT token
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("tenant_id", user.TenantId.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("full_name", user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"]!,
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60"))
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashPassword(string passowrd)
    {
        return BCrypt.Net.BCrypt.HashPassword(passowrd, workFactor: 12);
    }

    public bool VerifyPassword(string passowrd, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(passowrd, hash);
    }
}
