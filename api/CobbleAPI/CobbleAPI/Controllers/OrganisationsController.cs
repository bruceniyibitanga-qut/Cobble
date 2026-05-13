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
public class OrganisationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrganisationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganisationListItemDto>>> GetOrganisations()
    {
        var query = _context.Organisations
            .AsNoTracking()
            .Where(o => o.DeletedAt == null);

        query = ApplyOrganisationScope(query);

        var organisations = await query
            .OrderBy(o => o.Name)
            .Select(o => new OrganisationListItemDto(
                o.Id,
                o.Name,
                o.Industry != null ? o.Industry.Name : null,
                o.PartnershipStatus,
                o.SubmissionStatus,
                o.Contacts
                    .Where(c => c.DeletedAt == null && c.IsPrimary)
                    .Select(c => c.FirstName + " " + c.LastName)
                    .FirstOrDefault(),
                o.Contacts
                    .Where(c => c.DeletedAt == null && c.IsPrimary)
                    .Select(c => c.Email)
                    .FirstOrDefault(),
                o.Projects.Count(p => p.DeletedAt == null),
                o.ProjectApplications.Count(a =>
                    a.DeletedAt == null &&
                    a.ApplicationStatus == "pending" &&
                    a.ResultingProjectId == null),
                o.UpdatedAt
            ))
            .ToListAsync();

        return Ok(organisations);
    }

    private IQueryable<Organisation> ApplyOrganisationScope(IQueryable<Organisation> query)
    {
        if (User.IsInRole("admin"))
            return query;

        if (User.IsInRole("course_organiser"))
            return query.Where(o => o.SubmissionStatus == "approved");

        if (User.IsInRole("industry_partner") &&
            Guid.TryParse(User.FindFirstValue("organisation_id"), out var organisationId))
        {
            return query.Where(o => o.Id == organisationId);
        }

        return query.Where(_ => false);
    }
}
