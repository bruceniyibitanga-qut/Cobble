using Qut.PartnerForge.Api.Data;
using Qut.PartnerForge.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Qut.PartnerForge.Api.Controllers;

/// <summary>
/// Industry partner project proposals (applications) before they become <see cref="Project"/> records, with faculty and org scoping.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectApplicationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="ProjectApplicationsController"/>.
    /// </summary>
    /// <param name="context">Database context.</param>
    public ProjectApplicationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists applications that have not yet resulted in a project (<see cref="ProjectApplication.ResultingProjectId"/> is null), filtered by role.
    /// </summary>
    /// <param name="search">Optional proposed title substring.</param>
    /// <param name="status">Application status; defaults to <c>pending</c> when omitted or blank.</param>
    /// <param name="organisationId">Optional owning organisation.</param>
    /// <param name="semester">Optional proposed semester.</param>
    /// <param name="year">Optional proposed year.</param>
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
                a.ProposedMultipleTeams,
                a.ProposedDisciplineArea,
                a.ProposedSecondaryItDiscipline,
                a.ProposedDeliverables,
                a.StudentProjectAgreement,
                a.IpAssignmentRationale,
                a.ApplicationStatus,
                a.SubmittedAt
            ))
            .ToListAsync();

        return Ok(applications);
    }

    /// <summary>
    /// Loads a single application that is visible under the current role.
    /// </summary>
    /// <param name="id">Application identifier.</param>
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
                a.ProposedMultipleTeams,
                a.ProposedDisciplineArea,
                a.ProposedSecondaryItDiscipline,
                a.ProposedDeliverables,
                a.StudentProjectAgreement,
                a.IpAssignmentRationale,
                a.ApplicationStatus,
                a.SubmittedAt
            ))
            .FirstOrDefaultAsync();

        return application == null ? NotFound() : Ok(application);
    }

    /// <summary>
    /// Submits a new project application as <c>pending</c>. Partners may only target their organisation for writes.
    /// </summary>
    /// <param name="request">Proposed project and organisation linkage.</param>
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
            ProposedMultipleTeams = request.ProposedMultipleTeams,
            ProposedDisciplineArea = request.ProposedDisciplineArea,
            ProposedSecondaryItDiscipline = request.ProposedSecondaryItDiscipline,
            ProposedDeliverables = request.ProposedDeliverables,
            StudentProjectAgreement = request.StudentProjectAgreement,
            IpAssignmentRationale = request.IpAssignmentRationale,
            ApplicationStatus = "pending",
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProjectApplications.Add(application);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProjectApplication), new { id = application.Id }, new { id = application.Id });
    }

    /// <summary>
    /// Updates a <c>pending</c> application within the caller&apos;s write scope for organisations.
    /// </summary>
    /// <param name="id">Application identifier.</param>
    /// <param name="request">Replacement field values.</param>
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
        application.ProposedMultipleTeams = request.ProposedMultipleTeams;
        application.ProposedDisciplineArea = request.ProposedDisciplineArea;
        application.ProposedSecondaryItDiscipline = request.ProposedSecondaryItDiscipline;
        application.ProposedDeliverables = request.ProposedDeliverables;
        application.StudentProjectAgreement = request.StudentProjectAgreement;
        application.IpAssignmentRationale = request.IpAssignmentRationale;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes an application. Allowed for <c>admin</c> and <c>course_organiser</c>.
    /// </summary>
    /// <param name="id">Application identifier.</param>
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

    /// <summary>
    /// Restricts readable applications using role, faculty, approved organisations, or the partner organisation id claim.
    /// </summary>
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

    /// <summary>
    /// Organisations writable by the caller when creating or updating applications (partners constrained to own org).
    /// </summary>
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
