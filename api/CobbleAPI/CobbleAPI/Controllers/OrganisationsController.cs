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
    public async Task<ActionResult<IEnumerable<OrganisationListItemDto>>> GetOrganisations(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? industryId)
    {
        var query = _context.Organisations
            .AsNoTracking()
            .Where(o => o.DeletedAt == null);

        query = ApplyOrganisationScope(query);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.PartnershipStatus == status);

        if (industryId.HasValue)
            query = query.Where(o => o.IndustryId == industryId.Value);

        var organisations = await query
            .OrderBy(o => o.Name)
            .Select(o => new OrganisationListItemDto(
                o.Id,
                o.Name,
                o.Industry != null ? o.Industry.Name : null,
                o.IndustryId,
                o.Email,
                o.Website,
                o.Phone,
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

    [HttpGet("{id}")]
    public async Task<ActionResult<OrganisationListItemDto>> GetOrganisation(Guid id)
    {
        var org = await ApplyOrganisationScope(_context.Organisations.AsNoTracking())
            .Where(o => o.DeletedAt == null && o.Id == id)
            .Select(o => new OrganisationListItemDto(
                o.Id,
                o.Name,
                o.Industry != null ? o.Industry.Name : null,
                o.IndustryId,
                o.Email,
                o.Website,
                o.Phone,
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
            .FirstOrDefaultAsync();

        return org == null ? NotFound() : Ok(org);
    }

    [HttpPost]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<ActionResult<OrganisationListItemDto>> CreateOrganisation(SaveOrganisationRequest request)
    {
        var org = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            IndustryId = request.IndustryId,
            Email = request.Email,
            Website = request.Website,
            Phone = request.Phone,
            PartnershipStatus = request.PartnershipStatus,
            SubmissionStatus = "approved",
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Organisations.Add(org);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrganisation), new { id = org.Id }, new { id = org.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> UpdateOrganisation(Guid id, SaveOrganisationRequest request)
    {
        var org = await ApplyOrganisationScope(_context.Organisations)
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

        if (org == null) return NotFound();

        org.Name = request.Name;
        org.IndustryId = request.IndustryId;
        org.Email = request.Email;
        org.Website = request.Website;
        org.Phone = request.Phone;
        org.PartnershipStatus = request.PartnershipStatus;
        org.Notes = request.Notes;
        org.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteOrganisation(Guid id)
    {
        var org = await _context.Organisations
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

        if (org == null) return NotFound();

        org.DeletedAt = DateTime.UtcNow;
        org.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
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
