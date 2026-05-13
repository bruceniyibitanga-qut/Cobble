using CobbleAPI.Data;
using CobbleAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectListItemDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? semester,
        [FromQuery] int? year)
    {
        var query = _context.Projects
            .AsNoTracking()
            .Where(p => p.DeletedAt == null && p.Organisation!.DeletedAt == null);

        query = ApplyProjectScope(query);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Title.Contains(search));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(semester))
            query = query.Where(p => p.Semester == semester);

        if (year.HasValue)
            query = query.Where(p => p.Year == year.Value);

        var projects = await query
            .OrderByDescending(p => p.Year)
            .ThenBy(p => p.Semester)
            .ThenBy(p => p.Title)
            .Select(p => new ProjectListItemDto(
                p.Id,
                p.Title,
                p.Organisation!.Name,
                p.Faculty != null ? p.Faculty.Name : null,
                p.ProjectType,
                p.Semester,
                p.Year,
                p.Status,
                p.StartDate,
                p.EndDate,
                p.UpdatedAt
            ))
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetById(Guid id)
    {
        var project = await ApplyProjectScope(_context.Projects)
            .Include(p => p.Organisation)
            .Include(p => p.Faculty)
            .Include(p => p.Applications.Where(a => a.DeletedAt == null))
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (project == null) return NotFound();
        return Ok(project);
    }

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
        project.Organisation = null!;
        project.Faculty = null;

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Project updated)
    {
        var project = await ApplyProjectScope(_context.Projects)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (project == null) return NotFound();

        project.Title = updated.Title;
        project.Description = updated.Description;
        project.ProjectType = updated.ProjectType;
        project.Semester = updated.Semester;
        project.Year = updated.Year;
        project.Status = updated.Status;
        project.StartDate = updated.StartDate;
        project.EndDate = updated.EndDate;
        project.Notes = updated.Notes;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(project);
    }

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

    private IQueryable<Project> ApplyProjectScope(IQueryable<Project> query)
    {
        if (User.IsInRole("admin"))
            return query;

        if (User.IsInRole("course_organiser"))
        {
            if (int.TryParse(User.FindFirstValue("faculty_id"), out var facultyId))
                return query.Where(p => p.FacultyId == facultyId);

            return query.Where(p => p.Organisation!.SubmissionStatus == "approved");
        }

        if (User.IsInRole("industry_partner") &&
            Guid.TryParse(User.FindFirstValue("organisation_id"), out var organisationId))
        {
            return query.Where(p => p.OrganisationId == organisationId);
        }

        return query.Where(_ => false);
    }
}
