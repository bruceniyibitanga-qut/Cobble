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
    public async Task<ActionResult<IEnumerable<OrganisationListItemDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status)
    {
        var query = _context.Organisations
            .AsNoTracking()
            .Where(o => o.DeletedAt == null);

        query = ApplyOrganisationScope(query);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.PartnershipStatus == status);

        var organisations = await query
            .OrderBy(o => o.Name)
            .Select(o => new OrganisationListItemDto(
                o.Id,
                o.Name,
                o.Industry != null ? o.Industry.Name : null,
                o.Email,
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
    public async Task<ActionResult<Organisation>> GetById(Guid id)
    {
        var org = await ApplyOrganisationScope(_context.Organisations)
            .Include(o => o.Industry)
            .Include(o => o.Contacts.Where(c => c.DeletedAt == null))
            .Include(o => o.Projects.Where(p => p.DeletedAt == null))
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

        if (org == null) return NotFound();
        return Ok(org);
    }

    [HttpPost]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<ActionResult<Organisation>> Create([FromBody] Organisation org)
    {
        org.Id = Guid.NewGuid();
        org.CreatedAt = DateTime.UtcNow;
        org.UpdatedAt = DateTime.UtcNow;
        org.Industry = null;

        _context.Organisations.Add(org);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = org.Id }, org);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Organisation updated)
    {
        var org = await ApplyOrganisationScope(_context.Organisations)
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

        if (org == null) return NotFound();

        org.Name = updated.Name;
        org.IndustryId = updated.IndustryId;
        org.Website = updated.Website;
        org.Phone = updated.Phone;
        org.Email = updated.Email;
        org.PartnershipStatus = updated.PartnershipStatus;
        org.Notes = updated.Notes;
        org.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(org);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var org = await _context.Organisations
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

        if (org == null) return NotFound();

        org.DeletedAt = DateTime.UtcNow;
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
