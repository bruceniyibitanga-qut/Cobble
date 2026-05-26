using Qut.PartnerForge.Api.Data;
using Qut.PartnerForge.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Qut.PartnerForge.Api.Controllers;

/// <summary>
/// Scheduled events visible to admins and optionally scoped faculty course organisers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="EventsController"/>.
    /// </summary>
    /// <param name="context">Database context.</param>
    public EventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists events after applying visibility rules and filters.
    /// </summary>
    /// <param name="search">Optional name substring.</param>
    /// <param name="type">Optional <see cref="Event.EventType"/> match.</param>
    /// <param name="facultyId">Optional host faculty.</param>
    /// <param name="year">Filters by calendar year of <see cref="Event.EventDate"/>.</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventListItemDto>>> GetEvents(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] int? facultyId,
        [FromQuery] int? year)
    {
        var query = _context.Events
            .AsNoTracking()
            .Where(e => e.DeletedAt == null);

        query = ApplyEventScope(query);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.EventType == type);

        if (facultyId.HasValue)
            query = query.Where(e => e.FacultyId == facultyId.Value);

        if (year.HasValue)
            query = query.Where(e => e.EventDate.Year == year.Value);

        var events = await query
            .OrderByDescending(e => e.EventDate)
            .ThenBy(e => e.Name)
            .Select(e => new EventListItemDto(
                e.Id,
                e.Name,
                e.Description,
                e.EventType,
                e.EventDate,
                e.Location,
                e.FacultyId,
                e.Faculty != null ? e.Faculty.Name : null,
                e.Attendances.Count(a => a.DeletedAt == null),
                e.UpdatedAt
            ))
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Loads one event visible to the caller.
    /// </summary>
    /// <param name="id">Event identifier.</param>
    [HttpGet("{id}")]
    public async Task<ActionResult<EventListItemDto>> GetEvent(Guid id)
    {
        var item = await ApplyEventScope(_context.Events.AsNoTracking())
            .Where(e => e.DeletedAt == null && e.Id == id)
            .Select(e => new EventListItemDto(
                e.Id,
                e.Name,
                e.Description,
                e.EventType,
                e.EventDate,
                e.Location,
                e.FacultyId,
                e.Faculty != null ? e.Faculty.Name : null,
                e.Attendances.Count(a => a.DeletedAt == null),
                e.UpdatedAt
            ))
            .FirstOrDefaultAsync();

        return item == null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// Creates an event recording audit fields from the JWT subject.
    /// </summary>
    /// <param name="request">Persisted calendar item.</param>
    [HttpPost]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<ActionResult<EventListItemDto>> CreateEvent(SaveEventRequest request)
    {
        var item = new Event
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            EventType = request.EventType,
            EventDate = request.EventDate,
            Location = request.Location,
            FacultyId = request.FacultyId,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = CurrentUserId(),
            UpdatedBy = CurrentUserId()
        };

        _context.Events.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = item.Id }, new { id = item.Id });
    }

    /// <summary>
    /// Updates fields on a scoped event.
    /// </summary>
    /// <param name="id">Event identifier.</param>
    /// <param name="request">Replacement field values.</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> UpdateEvent(Guid id, SaveEventRequest request)
    {
        var item = await ApplyEventScope(_context.Events)
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);

        if (item == null) return NotFound();

        item.Name = request.Name;
        item.Description = request.Description;
        item.EventType = request.EventType;
        item.EventDate = request.EventDate;
        item.Location = request.Location;
        item.FacultyId = request.FacultyId;
        item.Notes = request.Notes;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = CurrentUserId();

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes an event. <c>admin</c> only.
    /// </summary>
    /// <param name="id">Event identifier.</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var item = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);

        if (item == null) return NotFound();

        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = CurrentUserId();

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Admins see all events; course organisers match their faculty or cross-faculty events (<c>FacultyId</c> null).
    /// </summary>
    private IQueryable<Event> ApplyEventScope(IQueryable<Event> query)
    {
        if (User.IsInRole("admin"))
            return query;

        if (User.IsInRole("course_organiser") &&
            int.TryParse(User.FindFirstValue("faculty_id"), out var facultyId))
        {
            return query.Where(e => e.FacultyId == facultyId || e.FacultyId == null);
        }

        return query;
    }

    /// <summary>
    /// Parses the current user identifier from JWT <see cref="ClaimTypes.NameIdentifier"/>.
    /// </summary>
    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
}
