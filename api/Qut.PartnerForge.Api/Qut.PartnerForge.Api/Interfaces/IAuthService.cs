using Qut.PartnerForge.Api.Models;

namespace Qut.PartnerForge.Api.Interfaces;

/// <summary>
/// JWT issuance and credential hashing used by authentication and user controllers.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Creates a signed RS256 bearer token embedding role, faculty, organisation, and subject claims.
    /// </summary>
    /// <param name="user">User loaded with navigations required by <see cref="Qut.PartnerForge.Api.Services.AuthService"/> (at least role name).</param>
    /// <returns>Serialized JWT compact string.</returns>
    string GenerateJwtToken(User user);

    /// <summary>
    /// Produces a BCrypt hash for storage alongside the user record.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Validates a plain-text password against a persisted BCrypt hash.
    /// </summary>
    bool VerifyPassword(string password, string hash);
}
