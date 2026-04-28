using CobbleAPI.Data;
using CobbleAPI.Interfaces;
using CobbleAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CobbleAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _auth;

    public AuthController(ApplicationDbContext context, IAuthService auth)
    {
        _context = context;
        _auth = auth;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.DeletedAt == null);

        if (user == null || !user.IsActive || !_auth.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(BuildAuthResponse(user));
    }

    /// <summary>Creates a new staff or admin account. Admin only.</summary>
    [HttpPost("register")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterUserRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.DeletedAt == null))
            return Conflict(new { message = "Email already registered." });

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
        if (role == null)
            return BadRequest(new { message = $"Invalid role '{request.Role}'. Valid values: admin, course_organiser, industry_partner." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _auth.HashPassword(request.Password),
            FullName = request.FullName,
            RoleId = role.Id,
            FacultyId = request.FacultyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        user.Role = role;
        return Ok(BuildAuthResponse(user));
    }

    private AuthResponse BuildAuthResponse(User user) => new(
        Token: _auth.GenerateJwtToken(user),
        Email: user.Email,
        FullName: user.FullName,
        Role: user.Role.Name,
        FacultyId: user.FacultyId,
        OrganisationId: user.OrganisationId
    );
}
