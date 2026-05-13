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
    public async Task<ActionResult<IEnumerable<ProjectApplicationListItemDto>>> GetProjectApplications(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] Guid? organisationId,
        [FromQuery] string? semester,
        [FromQuery] int? year)
    {
        var query = _context.ProjectApplications
            .AsNoTracking()
            .Where(a =>
                a.DeletedAt == null &&
                a.Organisation.DeletedAt == null &&
                a.ResultingProjectId == null);

        query = ApplyApplicationScope(query);

        query = query.Where(a => a.ApplicationStatus == (string.IsNullOrWhiteSpace(status) ? "pending" : status));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.ProposedTitle.Contains(search));

        if (organisationId.HasValue)
            query = query.Where(a => a.OrganisationId == organisationId.Value);

        if (!string.IsNullOrWhiteSpace(semester))
            query = query.Where(a => a.ProposedSemester == semester);

        if (year.HasValue)
            query = query.Where(a => a.ProposedYear == year.Value);

        var applications = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new ProjectApplicationListItemDto(
                a.Id,
                a.ProposedTitle,
                a.ProposedDescription,
                a.OrganisationId,
                a.Organisation.Name,
                a.ContactId,
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
    public async Task<ActionResult<ProjectApplicationListItemDto>> GetProjectApplication(Guid id)
    {
        var application = await ApplyApplicationScope(_context.ProjectApplications.AsNoTracking())
            .Where(a => a.DeletedAt == null && a.Organisation.DeletedAt == null && a.Id == id)
            .Select(a => new ProjectApplicationListItemDto(
                a.Id,
                a.ProposedTitle,
                a.ProposedDescription,
                a.OrganisationId,
                a.Organisation.Name,
                a.ContactId,
                a.Contact != null ? a.Contact.FirstName + " " + a.Contact.LastName : null,
                a.ProposedProjectType,
                a.ProposedSemester,
                a.ProposedYear,
                a.ProposedFaculty != null ? a.ProposedFaculty.Name : null,
                a.ApplicationStatus,
                a.SubmittedAt
            ))
            .FirstOrDefaultAsync();

        return application == null ? NotFound() : Ok(application);
    }

    [HttpPost]
    [Authorize(Roles = "admin,course_organiser,industry_partner")]
    public async Task<ActionResult<ProjectApplicationListItemDto>> CreateProjectApplication(SaveProjectApplicationRequest request)
    {
        if (!await ApplyOrganisationScopeForWrite(_context.Organisations).AnyAsync(o => o.Id == request.OrganisationId && o.DeletedAt == null))
            return BadRequest(new { message = "Invalid organisation." });

        var application = new ProjectApplication
        {
            Id = Guid.NewGuid(),
            OrganisationId = request.OrganisationId,
            ContactId = request.ContactId,
            ProposedTitle = request.ProposedTitle,
            ProposedDescription = request.ProposedDescription,
            ProposedProjectType = request.ProposedProjectType,
            ProposedSemester = request.ProposedSemester,
            ProposedYear = request.ProposedYear,
            ProposedFacultyId = request.ProposedFacultyId,
            ApplicationStatus = "pending",
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProjectApplications.Add(application);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProjectApplication), new { id = application.Id }, new { id = application.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,course_organiser,industry_partner")]
    public async Task<IActionResult> UpdateProjectApplication(Guid id, SaveProjectApplicationRequest request)
    {
        var application = await ApplyApplicationScope(_context.ProjectApplications)
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null && a.ApplicationStatus == "pending");

        if (application == null) return NotFound();

        if (!await ApplyOrganisationScopeForWrite(_context.Organisations).AnyAsync(o => o.Id == request.OrganisationId && o.DeletedAt == null))
            return BadRequest(new { message = "Invalid organisation." });

        application.OrganisationId = request.OrganisationId;
        application.ContactId = request.ContactId;
        application.ProposedTitle = request.ProposedTitle;
        application.ProposedDescription = request.ProposedDescription;
        application.ProposedProjectType = request.ProposedProjectType;
        application.ProposedSemester = request.ProposedSemester;
        application.ProposedYear = request.ProposedYear;
        application.ProposedFacultyId = request.ProposedFacultyId;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> DeleteProjectApplication(Guid id)
    {
        var application = await _context.ProjectApplications
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

        if (application == null) return NotFound();

        application.DeletedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

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

    private IQueryable<Organisation> ApplyOrganisationScopeForWrite(IQueryable<Organisation> query)
    {
        if (User.IsInRole("admin") || User.IsInRole("course_organiser"))
            return query;

        if (User.IsInRole("industry_partner") &&
            Guid.TryParse(User.FindFirstValue("organisation_id"), out var organisationId))
        {
            return query.Where(o => o.Id == organisationId);
        }

        return query.Where(_ => false);
    }
}
