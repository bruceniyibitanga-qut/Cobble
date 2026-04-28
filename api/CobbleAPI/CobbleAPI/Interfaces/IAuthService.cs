using CobbleAPI.Models;

namespace CobbleAPI.Interfaces;

public interface IAuthService
{
    string GenerateJwtToken(User user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
