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
    public async Task<ActionResult<IEnumerable<ProjectListItemDto>>> GetProjects()
    {
        var query = _context.Projects
            .AsNoTracking()
            .Where(p => p.DeletedAt == null && p.Organisation.DeletedAt == null);

        query = ApplyProjectScope(query);

        var projects = await query
            .OrderByDescending(p => p.Year)
            .ThenBy(p => p.Semester)
            .ThenBy(p => p.Title)
            .Select(p => new ProjectListItemDto(
                p.Id,
                p.Title,
                p.Organisation.Name,
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
