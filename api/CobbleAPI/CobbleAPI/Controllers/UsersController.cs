using CobbleAPI.Data;
using CobbleAPI.Interfaces;
using CobbleAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CobbleAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _auth;

    public UsersController(ApplicationDbContext context, IAuthService auth)
    {
        _context = context;
        _auth = auth;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserListItemDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] bool? active)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role.Name == role);

        if (active.HasValue)
            query = query.Where(u => u.IsActive == active.Value);

        var users = await query
            .OrderBy(u => u.FullName)
            .Select(u => new UserListItemDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Role.Name,
                u.FacultyId,
                u.Faculty != null ? u.Faculty.Name : null,
                u.OrganisationId,
                u.Organisation != null ? u.Organisation.Name : null,
                u.IsActive,
                u.LastLoginAt,
                u.UpdatedAt
            ))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserListItemDto>> GetUser(Guid id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.DeletedAt == null && u.Id == id)
            .Select(u => new UserListItemDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Role.Name,
                u.FacultyId,
                u.Faculty != null ? u.Faculty.Name : null,
                u.OrganisationId,
                u.Organisation != null ? u.Organisation.Name : null,
                u.IsActive,
                u.LastLoginAt,
                u.UpdatedAt
            ))
            .FirstOrDefaultAsync();

        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserListItemDto>> CreateUser(SaveUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Password is required." });

        if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.DeletedAt == null))
            return Conflict(new { message = "Email already registered." });

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
        if (role == null)
            return BadRequest(new { message = "Invalid role." });

        if (!await ReferencesExist(request.FacultyId, request.OrganisationId))
            return BadRequest(new { message = "Invalid faculty or organisation." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _auth.HashPassword(request.Password),
            FullName = request.FullName,
            RoleId = role.Id,
            FacultyId = request.FacultyId,
            OrganisationId = request.OrganisationId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = CurrentUserId(),
            UpdatedBy = CurrentUserId()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { id = user.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, SaveUserRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

        if (user == null) return NotFound();

        if (await _context.Users.AnyAsync(u => u.Id != id && u.Email == request.Email && u.DeletedAt == null))
            return Conflict(new { message = "Email already registered." });

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
        if (role == null)
            return BadRequest(new { message = "Invalid role." });

        if (!await ReferencesExist(request.FacultyId, request.OrganisationId))
            return BadRequest(new { message = "Invalid faculty or organisation." });

        if (CurrentUserId() == id && !request.IsActive)
            return BadRequest(new { message = "You cannot deactivate your own account." });

        user.Email = request.Email;
        user.FullName = request.FullName;
        user.RoleId = role.Id;
        user.FacultyId = request.FacultyId;
        user.OrganisationId = request.OrganisationId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = CurrentUserId();

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = _auth.HashPassword(request.Password);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

        if (user == null) return NotFound();

        if (CurrentUserId() == id)
            return BadRequest(new { message = "You cannot delete your own account." });

        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = CurrentUserId();

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> ReferencesExist(int? facultyId, Guid? organisationId)
    {
        if (facultyId.HasValue && !await _context.Faculties.AnyAsync(f => f.Id == facultyId.Value))
            return false;

        if (organisationId.HasValue && !await _context.Organisations.AnyAsync(o => o.Id == organisationId.Value && o.DeletedAt == null))
            return false;

        return true;
    }

    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
}
