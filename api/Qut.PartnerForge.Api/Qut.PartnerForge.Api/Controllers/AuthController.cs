using Qut.PartnerForge.Api.Data;
using Qut.PartnerForge.Api.Interfaces;
using Qut.PartnerForge.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Qut.PartnerForge.Api.Controllers;

/// <summary>
/// JWT authentication endpoints: login for all users and admin-only registration of staff accounts.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _auth;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthController"/>.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="auth">Password hashing and token issuance.</param>
    public AuthController(ApplicationDbContext context, IAuthService auth)
    {
        _context = context;
        _auth = auth;
    }

    /// <summary>
    /// Authenticates a user by email and password and returns a signed JWT plus profile claims.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>An <see cref="AuthResponse"/> with a bearer token when credentials are valid.</returns>
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

    /// <summary>
    /// Creates a new staff account (course organiser or industry partner) or admin. Requires the <c>admin</c> role.
    /// </summary>
    /// <param name="request">Registration payload including role and optional faculty.</param>
    /// <returns>An <see cref="AuthResponse"/> with a token for the new user.</returns>
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

    /// <summary>
    /// Builds the API authentication payload from the persisted user entity and current JWT settings.
    /// </summary>
    private AuthResponse BuildAuthResponse(User user) => new(
        Token: _auth.GenerateJwtToken(user),
        Email: user.Email,
        FullName: user.FullName,
        Role: user.Role.Name,
        FacultyId: user.FacultyId,
        OrganisationId: user.OrganisationId
    );
}
