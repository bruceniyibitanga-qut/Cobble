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
    public async Task<ActionResult<IEnumerable<ProjectListItemDto>>> GetProjects(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? semester,
        [FromQuery] int? year,
        [FromQuery] Guid? organisationId)
    {
        var query = _context.Projects
            .AsNoTracking()
            .Where(p => p.DeletedAt == null && p.Organisation.DeletedAt == null);

        query = ApplyProjectScope(query);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Title.Contains(search));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(semester))
            query = query.Where(p => p.Semester == semester);

        if (year.HasValue)
            query = query.Where(p => p.Year == year.Value);

        if (organisationId.HasValue)
            query = query.Where(p => p.OrganisationId == organisationId.Value);

        var projects = await query
            .OrderByDescending(p => p.Year)
            .ThenBy(p => p.Semester)
            .ThenBy(p => p.Title)
            .Select(p => new ProjectListItemDto(
                p.Id,
                p.Title,
                p.Description,
                p.OrganisationId,
                p.Organisation.Name,
                p.FacultyId,
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
    public async Task<ActionResult<ProjectListItemDto>> GetProject(Guid id)
    {
        var project = await ApplyProjectScope(_context.Projects.AsNoTracking())
            .Where(p => p.DeletedAt == null && p.Organisation.DeletedAt == null && p.Id == id)
            .Select(p => new ProjectListItemDto(
                p.Id,
                p.Title,
                p.Description,
                p.OrganisationId,
                p.Organisation.Name,
                p.FacultyId,
                p.Faculty != null ? p.Faculty.Name : null,
                p.ProjectType,
                p.Semester,
                p.Year,
                p.Status,
                p.StartDate,
                p.EndDate,
                p.UpdatedAt
            ))
            .FirstOrDefaultAsync();

        return project == null ? NotFound() : Ok(project);
    }

    [HttpPost]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<ActionResult<ProjectListItemDto>> CreateProject(SaveProjectRequest request)
    {
        if (!await _context.Organisations.AnyAsync(o => o.Id == request.OrganisationId && o.DeletedAt == null))
            return BadRequest(new { message = "Invalid organisation." });

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            OrganisationId = request.OrganisationId,
            FacultyId = request.FacultyId,
            ProjectType = request.ProjectType,
            Semester = request.Semester,
            Year = request.Year,
            Status = request.Status,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, new { id = project.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> UpdateProject(Guid id, SaveProjectRequest request)
    {
        var project = await ApplyProjectScope(_context.Projects)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (project == null) return NotFound();

        if (!await _context.Organisations.AnyAsync(o => o.Id == request.OrganisationId && o.DeletedAt == null))
            return BadRequest(new { message = "Invalid organisation." });

        project.Title = request.Title;
        project.Description = request.Description;
        project.OrganisationId = request.OrganisationId;
        project.FacultyId = request.FacultyId;
        project.ProjectType = request.ProjectType;
        project.Semester = request.Semester;
        project.Year = request.Year;
        project.Status = request.Status;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Notes = request.Notes;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (project == null) return NotFound();

        project.DeletedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;

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

            return query.Where(p => p.Organisation.SubmissionStatus == "approved");
        }

        if (User.IsInRole("industry_partner") &&
            Guid.TryParse(User.FindFirstValue("organisation_id"), out var organisationId))
        {
            return query.Where(p => p.OrganisationId == organisationId);
        }

        return query.Where(_ => false);
    }
}
