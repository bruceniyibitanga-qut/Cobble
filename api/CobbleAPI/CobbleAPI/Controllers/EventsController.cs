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
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EventsController(ApplicationDbContext context)
    {
        _context = context;
    }

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

    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
}
