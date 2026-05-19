using Qut.PartnerForge.Api.Interfaces;
using Qut.PartnerForge.Api.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Qut.PartnerForge.Api.Services;

/// <summary>
/// Default implementation of <see cref="IAuthService"/> using PKCS#8 RSA keys from configuration for JWT signing.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthService"/>.
    /// </summary>
    /// <param name="configuration">Application configuration containing JWT and PEM secrets.</param>
    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public string GenerateJwtToken(User user)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(_configuration["Jwt:PrivateKeyPem"]!);
        var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.Name),
            new("full_name", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (user.FacultyId.HasValue)
            claims.Add(new Claim("faculty_id", user.FacultyId.Value.ToString()));

        if (user.OrganisationId.HasValue)
            claims.Add(new Claim("organisation_id", user.OrganisationId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"]!,
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
