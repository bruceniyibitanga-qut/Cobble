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
public class ProjectApplicationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProjectApplicationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectApplicationListItemDto>>> GetAll(
        [FromQuery] string? status)
    {
        var query = _context.ProjectApplications
            .AsNoTracking()
            .Where(a =>
                a.DeletedAt == null &&
                a.Organisation.DeletedAt == null &&
                a.ResultingProjectId == null);

        query = ApplyApplicationScope(query);
        query = query.Where(a => a.ApplicationStatus == (string.IsNullOrWhiteSpace(status) ? "pending" : status));

        var applications = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new ProjectApplicationListItemDto(
                a.Id,
                a.ProposedTitle,
                a.Organisation.Name,
                a.Contact != null ? a.Contact.FirstName + " " + a.Contact.LastName : null,
                a.ProposedProjectType,
                a.ProposedSemester,
                a.ProposedYear,
                a.ProposedFaculty != null ? a.ProposedFaculty.Name : null,
                a.ApplicationStatus,
                a.SubmittedAt
            ))
            .ToListAsync();

        return Ok(applications);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectApplication>> GetById(Guid id)
    {
        var app = await ApplyApplicationScope(_context.ProjectApplications)
            .Include(a => a.Organisation)
            .Include(a => a.Contact)
            .Include(a => a.ResultingProject)
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

        if (app == null) return NotFound();
        return Ok(app);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectApplication>> Create([FromBody] ProjectApplication app)
    {
        app.Id = Guid.NewGuid();
        app.ApplicationStatus = "pending";
        app.SubmittedAt = DateTime.UtcNow;
        app.CreatedAt = DateTime.UtcNow;
        app.UpdatedAt = DateTime.UtcNow;
        app.Organisation = null!;
        app.Contact = null;
        app.ProposedFaculty = null;
        app.ResultingProject = null;

        _context.ProjectApplications.Add(app);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = app.Id }, app);
    }

    [HttpPut("{id}/review")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewRequest request)
    {
        var app = await ApplyApplicationScope(_context.ProjectApplications)
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

        if (app == null) return NotFound();
        if (request.Status is not ("approved" or "rejected"))
            return BadRequest(new { message = "Status must be approved or rejected." });

        app.ApplicationStatus = request.Status;
        app.ReviewNotes = request.Notes;
        app.ReviewedAt = DateTime.UtcNow;
        app.UpdatedAt = DateTime.UtcNow;

        if (request.Status == "approved")
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = app.ProposedTitle,
                Description = app.ProposedDescription,
                OrganisationId = app.OrganisationId,
                FacultyId = app.ProposedFacultyId,
                ProjectType = app.ProposedProjectType ?? "capstone",
                Semester = app.ProposedSemester ?? "S1",
                Year = app.ProposedYear ?? DateTime.UtcNow.Year,
                Status = "proposed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            app.ResultingProjectId = project.Id;
        }

        await _context.SaveChangesAsync();
        return Ok(app);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var app = await _context.ProjectApplications
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

        if (app == null) return NotFound();

        app.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<ProjectApplication> ApplyApplicationScope(IQueryable<ProjectApplication> query)
    {
        if (User.IsInRole("admin"))
            return query;

        if (User.IsInRole("course_organiser"))
        {
            if (int.TryParse(User.FindFirstValue("faculty_id"), out var facultyId))
                return query.Where(a => a.ProposedFacultyId == facultyId);

            return query.Where(a => a.Organisation.SubmissionStatus == "approved");
        }

        if (User.IsInRole("industry_partner") &&
            Guid.TryParse(User.FindFirstValue("organisation_id"), out var organisationId))
        {
            return query.Where(a => a.OrganisationId == organisationId);
        }

        return query.Where(_ => false);
    }
}

public class ReviewRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
