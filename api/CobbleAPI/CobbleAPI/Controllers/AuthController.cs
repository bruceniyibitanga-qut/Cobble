using CobbleAPI.Data;
using CobbleAPI.Interfaces;
using CobbleAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CobbleAPI.Controllers;

[Route("api/[controller]")]
[Authorize]
public class AuthController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _auth;

    public AuthController(ApplicationDbContext context, IAuthService auth)
    {
        _context = context;
        _auth = auth;

    }

    /// <summary>
    /// Creates a new tenant (organisation). Only accessible by admins.
    /// </summary>
    /// <param name="orgRequest"></param>
    /// <returns></returns>

    [HttpPost("register/organisation")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TenantResponse>> RegisterOrganisation(RegisterOrganisationRequest orgRequest)
    {
        if(await _context.Tenant.AnyAsync(t => t.Name == orgRequest.TenantName)) return Conflict("Tenant name already exists");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = orgRequest.TenantName
        };

        _context.Tenant.Add(tenant);
        await _context.SaveChangesAsync();

        return Ok(new TenantResponse(
            Id: tenant.Id,
            Name: tenant.Name,
            UserCount: tenant.Users.Count,
            AdminCount: tenant.AdminCount(),
            CreatedAt: tenant.CreatedAt
        ));
    }

    [HttpPost("register/user")]
    public async Task<ActionResult<AuthResponse>> RegisterUser(JoinTenantRequest request)
    {
        var tenant = await _context.Tenant.FindAsync(request.TenantId);
        if (tenant == null)
            return NotFound(new { message = "Organisation not found." });

        var result = await CreateUser(
            request.Email, request.Password, request.FullName, tenant, "Member");

        if (result.Result is not OkObjectResult)
            return result;

        await _context.SaveChangesAsync();
        return result;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _context.User
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !_auth.VerifyPassword(request.Password, user.PasswordHash)) 
            return Unauthorized(new { message = "Invalid email or password." });

        var token = _auth.GenerateJwtToken(user);

        return Ok(new AuthResponse(
            Token: token,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role,
            TenantId: user.TenantId,
            TenantName: user.Tenant.Name
        ));
    }

    [AllowAnonymous]
    private async Task<ActionResult<AuthResponse>> CreateUser(
    string email, string password, string fullName, Tenant tenant, string role)
    {
        if (await _context.User.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Email already registered." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _auth.HashPassword(password),
            FullName = fullName,
            Role = role,
            TenantId = tenant.Id
        };

        _context.User.Add(user);

        var token = _auth.GenerateJwtToken(user);

        return Ok(new AuthResponse(
            Token: token,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role,
            TenantId: tenant.Id,
            TenantName: tenant.Name
        ));
    }

}
