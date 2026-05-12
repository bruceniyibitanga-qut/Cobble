using CobbleAPI.Models;
using CobbleAPI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CobbleAPI.Tests;

public class AuthServiceTests
{
    private readonly AuthService _auth;
    private readonly RsaSecurityKey _publicKey;
    private const string TEST_EMAIL = "test@cobble.com";
    private const string TEST_PASSWORD = "Password123!";
    private const string TEST_USER = "John Smith";

    public AuthServiceTests()
    {
        using var rsa = RSA.Create(2048);
        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
        var publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        _publicKey = new RsaSecurityKey(RSA.Create());
        _publicKey.Rsa.ImportFromPem(publicKeyPem);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:PrivateKeyPem"] = privateKeyPem,
            ["Jwt:PublicKeyPem"] = publicKeyPem,
            ["Jwt:Issuer"] = "CobbleAPITest",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpiryInMinutes"] = "60"
        }).Build();

        _auth = new AuthService(config);
    }

    [Fact]
    public void HashPassword_ReturnsVerifiableHash()
    {
        var hashed = _auth.HashPassword(TEST_PASSWORD);

        var isVerified = _auth.VerifyPassword(TEST_PASSWORD, hashed);

        Assert.NotNull(hashed);
        Assert.NotEqual(TEST_PASSWORD, hashed);
        Assert.True(isVerified);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        // Arrange
        var hashed = _auth.HashPassword(TEST_PASSWORD);
        // Act
        var isVerified = _auth.VerifyPassword(TEST_PASSWORD, hashed);
        // Assert
        Assert.True(isVerified);
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = _auth.HashPassword(TEST_PASSWORD);

        Assert.False(_auth.VerifyPassword("wrongpassword", hash));
    }

    [Fact]
    public void GenerateJwtToken_ContainsExpectedClaims()
    {
        var orgId = Guid.NewGuid();
        var user = CreateUser(roleName: "admin", facultyId: 2, organisationId: orgId);

        var token = _auth.GenerateJwtToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.First(x => x.Type == "sub").Value);
        Assert.Equal(user.Email, jwt.Claims.First(x => x.Type == "email").Value);
        Assert.Equal(user.FullName, jwt.Claims.First(x => x.Type == "full_name").Value);
        Assert.Equal(user.Role.Name, jwt.Claims.First(x => x.Type == ClaimTypes.Role).Value);
        Assert.Equal(user.FacultyId.ToString(), jwt.Claims.First(x => x.Type == "faculty_id").Value);
        Assert.Equal(user.OrganisationId.ToString(), jwt.Claims.First(x => x.Type == "organisation_id").Value);
    }

    [Fact]
    public void GenerateJwtToken_OmitsOptionalTenantClaims_WhenUserHasNoTenantIds()
    {
        var user = CreateUser(facultyId: null, organisationId: null);

        var token = _auth.GenerateJwtToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == "faculty_id");
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == "organisation_id");
    }

    [Fact]
    public void GenerateJwtToken_CanBeValidatedWithConfiguredPublicKey()
    {
        var user = CreateUser();

        var token = _auth.GenerateJwtToken(user);
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "CobbleAPITest",
            ValidAudience = "TestAudience",
            IssuerSigningKey = _publicKey,
            ClockSkew = TimeSpan.Zero
        }, out var validatedToken);

        Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal(user.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(user.Email, principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(user.Role.Name, principal.FindFirstValue(ClaimTypes.Role));
    }

    [Fact]
    public void GenerateJwtToken_ExpiresInFuture()
    {
        var user = CreateUser();

        var token = _auth.GenerateJwtToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("CobbleAPITest", jwt.Issuer);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }

    private static User CreateUser(
        string roleName = "admin",
        int? facultyId = 2,
        Guid? organisationId = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = TEST_EMAIL,
            FullName = TEST_USER,
            RoleId = 1,
            FacultyId = facultyId,
            OrganisationId = organisationId,
            Role = new Role { Id = 1, Name = roleName },
            Faculty = facultyId.HasValue ? new Faculty { Id = facultyId.Value, Name = "Engineering" } : null,
            Organisation = organisationId.HasValue
                ? new Organisation { Id = organisationId.Value, Name = "Cobble Inc." }
                : null
        };
    }
}
