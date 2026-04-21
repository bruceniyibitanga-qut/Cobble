using CobbleAPI.Models;

namespace CobbleAPI.Interfaces;

public interface IAuthService
{
    string GenerateJwtToken(User user);
    string HashPassword(string passowrd);
    bool VerifyPassword(string passowrd, string hash);
}
