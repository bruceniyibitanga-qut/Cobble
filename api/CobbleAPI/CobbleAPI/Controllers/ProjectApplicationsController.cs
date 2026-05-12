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
    public async Task<ActionResult<IEnumerable<ProjectApplicationListItemDto>>> GetProjectApplications()
    {
        var query = _context.ProjectApplications
            .AsNoTracking()
            .Where(a => a.DeletedAt == null && a.Organisation.DeletedAt == null);

        query = ApplyApplicationScope(query);

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
