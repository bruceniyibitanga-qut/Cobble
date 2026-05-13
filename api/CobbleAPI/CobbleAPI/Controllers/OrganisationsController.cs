using CobbleAPI.Data;
using CobbleAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CobbleAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganisationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrganisationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 获取所有组织（支持搜索）
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Organisation>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status)
    {
        var query = _context.Organisations
            .Where(o => o.DeletedAt == null)
            .Include(o => o.Industry)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(o => o.Name.Contains(search));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.PartnershipStatus == status);

        return Ok(await query.ToListAsync());
    }

    // 获取单个组织
    [HttpGet("{id}")]
    public async Task<ActionResult<Organisation>> GetById(Guid id)
    {
        var org = await _context.Organisations
            .Include(o => o.Industry)
            .Include(o => o.Contacts)
            .Include(o => o.Projects)
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

        if (org == null) return NotFound();
        return Ok(org);
    }

    // 新增组织
    [HttpPost]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<ActionResult<Organisation>> Create(Organisation org)
    {
        org.Id = Guid.NewGuid();
        org.CreatedAt = DateTime.UtcNow;
        org.UpdatedAt = DateTime.UtcNow;

        _context.Organisations.Add(org);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = org.Id }, org);
    }

    // 编辑组织
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> Update(Guid id, Organisation updated)
    {
        var org = await _context.Organisations
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

        if (org == null) return NotFound();

        org.Name = updated.Name;
        org.Website = updated.Website;
        org.Phone = updated.Phone;
        org.Email = updated.Email;
        org.PartnershipStatus = updated.PartnershipStatus;
        org.Notes = updated.Notes;
        org.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(org);
    }

    // 删除组织（软删除）
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var org = await _context.Organisations
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

        if (org == null) return NotFound();

        org.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}