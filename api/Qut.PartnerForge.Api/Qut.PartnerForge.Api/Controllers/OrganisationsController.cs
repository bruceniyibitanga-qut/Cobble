using Qut.PartnerForge.Api.Data;
using Qut.PartnerForge.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Qut.PartnerForge.Api.Controllers;

/// <summary>
/// Organisation directory with partnership and submission metadata plus aggregate counts for projects and pending applications.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganisationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="OrganisationsController"/>.
    /// </summary>
    /// <param name="context">Database context.</param>
    public OrganisationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists industry lookup values for organisation forms.
    /// </summary>
    [HttpGet("industries")]
    public async Task<ActionResult<IEnumerable<object>>> GetIndustries()
    {
        var industries = await _context.Industries
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new { i.Id, i.Name })
            .ToListAsync();

        return Ok(industries);
    }

    /// <summary>
    /// Lists organisations accessible to the caller with optional filters.
    /// </summary>
    /// <param name="search">Optional name substring filter.</param>
    /// <param name="status">Optional <see cref="Organisation.PartnershipStatus"/> value.</param>
    /// <param name="industryId">Optional industry foreign key.</param>
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
                o.RegistrationId,
                o.Name,
                o.Abn,
                o.Industry != null ? o.Industry.Name : null,
                o.IndustryId,
                o.Email,
                o.Website,
                o.Phone,
                o.AddressLine1,
                o.AddressLine2,
                o.City,
                o.State,
                o.Postcode,
                o.Country,
                o.OrganisationInformation,
                o.PartnershipStatus,
                o.SubmissionStatus,
                o.Contacts
                    .Where(c => c.DeletedAt == null && c.IsPrimary)
                    .Select(c => c.FirstName + " " + c.LastName)
                    .FirstOrDefault(),
                o.Contacts
                    .Where(c => c.DeletedAt == null && c.IsPrimary)
                    .Select(c => c.JobTitle)
                    .FirstOrDefault(),
                o.Contacts
                    .Where(c => c.DeletedAt == null && c.IsPrimary)
                    .Select(c => c.Email)
                    .FirstOrDefault(),
                o.Contacts
                    .Where(c => c.DeletedAt == null && c.IsPrimary)
                    .Select(c => c.Phone)
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

    /// <summary>
    /// Returns one organisation row when visible to the current user.
    /// </summary>
    /// <param name="id">Organisation identifier.</param>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrganisationListItemDto>> GetOrganisation(Guid id)
    {
        var org = await ApplyOrganisationScope(_context.Organisations.AsNoTracking())
            .Where(o => o.DeletedAt == null && o.Id == id)
            .Select(o => new OrganisationListItemDto(
                o.Id,
                o.RegistrationId,
                o.Name,
                o.Abn,
                o.Industry != null ? o.Industry.Name : null,
                o.IndustryId,
                o.Email,
                o.Website,
                o.Phone,
                o.AddressLine1,
                o.AddressLine2,
                o.City,
                o.State,
                o.Postcode,
                o.Country,
                o.OrganisationInformation,
                o.PartnershipStatus,
                o.SubmissionStatus,
                o.Contacts
                    .Where(c => c.DeletedAt == null && c.IsPrimary)
                    .Select(c => c.FirstName + " " + c.LastName)
                    .FirstOrDefault(),
                o.Contacts
                    .Where(c => c.DeletedAt == null && c.IsPrimary)
                    .Select(c => c.JobTitle)
                    .FirstOrDefault(),
                o.Contacts
                    .Where(c => c.DeletedAt == null && c.IsPrimary)
                    .Select(c => c.Email)
                    .FirstOrDefault(),
                o.Contacts
                    .Where(c => c.DeletedAt == null && c.IsPrimary)
                    .Select(c => c.Phone)
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

    /// <summary>
    /// Creates an organisation with <see cref="Organisation.SubmissionStatus"/> defaulted to approved.
    /// </summary>
    /// <param name="request">Persisted organisation fields.</param>
    [HttpPost]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<ActionResult<OrganisationListItemDto>> CreateOrganisation(SaveOrganisationRequest request)
    {
        var org = new Organisation
        {
            Id = Guid.NewGuid(),
            RegistrationId = request.RegistrationId,
            Name = request.Name,
            Abn = request.Abn,
            IndustryId = request.IndustryId,
            Email = request.Email,
            Website = request.Website,
            Phone = request.Phone,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            Postcode = request.Postcode,
            Country = string.IsNullOrWhiteSpace(request.Country) ? "Australia" : request.Country,
            OrganisationInformation = request.OrganisationInformation,
            PartnershipStatus = request.PartnershipStatus,
            SubmissionStatus = "approved",
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Organisations.Add(org);
        UpsertPrimaryContact(org.Id, request);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrganisation), new { id = org.Id }, new { id = org.Id });
    }

    /// <summary>
    /// Updates organisation details within the caller&apos;s visibility scope.
    /// </summary>
    /// <param name="id">Organisation identifier.</param>
    /// <param name="request">Replacement field values.</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> UpdateOrganisation(Guid id, SaveOrganisationRequest request)
    {
        var org = await ApplyOrganisationScope(_context.Organisations)
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

        if (org == null) return NotFound();

        org.RegistrationId = request.RegistrationId;
        org.Name = request.Name;
        org.Abn = request.Abn;
        org.IndustryId = request.IndustryId;
        org.Email = request.Email;
        org.Website = request.Website;
        org.Phone = request.Phone;
        org.AddressLine1 = request.AddressLine1;
        org.AddressLine2 = request.AddressLine2;
        org.City = request.City;
        org.State = request.State;
        org.Postcode = request.Postcode;
        org.Country = string.IsNullOrWhiteSpace(request.Country) ? "Australia" : request.Country;
        org.OrganisationInformation = request.OrganisationInformation;
        org.PartnershipStatus = request.PartnershipStatus;
        org.Notes = request.Notes;
        org.UpdatedAt = DateTime.UtcNow;

        UpsertPrimaryContact(org.Id, request);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes an organisation. <c>admin</c> only.
    /// </summary>
    /// <param name="id">Organisation identifier.</param>
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

    /// <summary>
    /// Restricts organisations to admin (all), course organiser (approved only), or the partner&apos;s own record.
    /// </summary>
    /// <param name="query">Organisations query before filters.</param>
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

    private void UpsertPrimaryContact(Guid organisationId, SaveOrganisationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PrimaryContactName) &&
            string.IsNullOrWhiteSpace(request.PrimaryContactEmail) &&
            string.IsNullOrWhiteSpace(request.PrimaryContactPhone) &&
            string.IsNullOrWhiteSpace(request.PrimaryContactPosition))
        {
            return;
        }

        var contact = _context.Contacts
            .FirstOrDefault(c => c.OrganisationId == organisationId && c.IsPrimary && c.DeletedAt == null);
        if (contact == null)
        {
            contact = new Contact
            {
                Id = Guid.NewGuid(),
                OrganisationId = organisationId,
                IsPrimary = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Contacts.Add(contact);
        }

        var (firstName, lastName) = SplitContactName(request.PrimaryContactName);
        contact.FirstName = firstName;
        contact.LastName = lastName;
        contact.JobTitle = request.PrimaryContactPosition;
        contact.Email = request.PrimaryContactEmail;
        contact.Phone = request.PrimaryContactPhone;
        contact.UpdatedAt = DateTime.UtcNow;
    }

    private static (string FirstName, string LastName) SplitContactName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return ("Unknown", "Contact");

        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 ? (parts[0], "") : (parts[0], parts[1]);
    }
}
