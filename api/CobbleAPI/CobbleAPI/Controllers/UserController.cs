using CobbleAPI.Data;
using CobbleAPI.Models;
using CobbleAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    // 获取所有用户
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        var users = await _context.Users
            .Where(u => u.DeletedAt == null)
            .Include(u => u.Role)
            .Include(u => u.Faculty)
            .ToListAsync();

        return Ok(users);
    }

    // 获取单个用户
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetById(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Faculty)
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

        if (user == null) return NotFound();
        return Ok(user);
    }

    // 修改用户角色和状态
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

        if (user == null) return NotFound();

        if (!string.IsNullOrEmpty(request.Role))
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
            if (role == null) return BadRequest(new { message = "Invalid role." });
            user.RoleId = role.Id;
        }

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        if (!string.IsNullOrEmpty(request.FullName))
            user.FullName = request.FullName;

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(user);
    }

    // 停用用户（软删除）
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

        if (user == null) return NotFound();

        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class UpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}