using CobbleAPI.Data;
using CobbleAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CobbleAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProjectsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 获取所有项目（支持搜索和筛选）
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? semester,
        [FromQuery] int? year)
    {
        var query = _context.Projects
            .Where(p => p.DeletedAt == null)
            .Include(p => p.Organisation)
            .Include(p => p.Faculty)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Title.Contains(search));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrEmpty(semester))
            query = query.Where(p => p.Semester == semester);

        if (year.HasValue)
            query = query.Where(p => p.Year == year.Value);

        return Ok(await query.ToListAsync());
    }

    // 获取单个项目
    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetById(Guid id)
    {
        var project = await _context.Projects
            .Include(p => p.Organisation)
            .Include(p => p.Faculty)
            .Include(p => p.Applications)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (project == null) return NotFound();
        return Ok(project);
    }

    // 新增项目
  [HttpPost]
[Authorize(Roles = "admin,course_organiser")]
public async Task<ActionResult<Project>> Create([FromBody] Project project)
{
    var org = await _context.Organisations
        .FirstOrDefaultAsync(o => o.Id == project.OrganisationId && o.DeletedAt == null);

    if (org == null)
        return BadRequest(new { message = "Invalid OrganisationId." });

    project.Id = Guid.NewGuid();
    project.CreatedAt = DateTime.UtcNow;
    project.UpdatedAt = DateTime.UtcNow;
    project.Organisation = null;
    project.Faculty = null;

    _context.Projects.Add(project);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
}
    // 编辑项目
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> Update(Guid id, Project updated)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (project == null) return NotFound();

        project.Title = updated.Title;
        project.Description = updated.Description;
        project.Status = updated.Status;
        project.Semester = updated.Semester;
        project.Year = updated.Year;
        project.StartDate = updated.StartDate;
        project.EndDate = updated.EndDate;
        project.Notes = updated.Notes;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(project);
    }

    // 删除项目（软删除）
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (project == null) return NotFound();

        project.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}