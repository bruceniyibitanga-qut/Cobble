using CobbleAPI.Data;
using CobbleAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    // 获取所有活动
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetAll(
        [FromQuery] string? type)
    {
        var query = _context.Events
            .Where(e => e.DeletedAt == null)
            .Include(e => e.Faculty)
            .AsQueryable();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(e => e.EventType == type);

        return Ok(await query.OrderByDescending(e => e.EventDate).ToListAsync());
    }

    // 获取单个活动
    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetById(Guid id)
    {
        var ev = await _context.Events
            .Include(e => e.Faculty)
            .Include(e => e.Attendances)
                .ThenInclude(a => a.Organisation)
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);

        if (ev == null) return NotFound();
        return Ok(ev);
    }

    // 新增活动
    [HttpPost]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<ActionResult<Event>> Create([FromBody] Event ev)
    {
        ev.Id = Guid.NewGuid();
        ev.CreatedAt = DateTime.UtcNow;
        ev.UpdatedAt = DateTime.UtcNow;
        ev.Faculty = null;

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, ev);
    }

    // 编辑活动
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Event updated)
    {
        var ev = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);

        if (ev == null) return NotFound();

        ev.Name = updated.Name;
        ev.Description = updated.Description;
        ev.EventType = updated.EventType;
        ev.EventDate = updated.EventDate;
        ev.Location = updated.Location;
        ev.Notes = updated.Notes;
        ev.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ev);
    }

    // 删除活动（软删除）
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ev = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);

        if (ev == null) return NotFound();

        ev.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // 添加出席记录
    [HttpPost("{id}/attendances")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> AddAttendance(Guid id, [FromBody] EventAttendance attendance)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);
        if (ev == null) return NotFound();

        attendance.Id = Guid.NewGuid();
        attendance.EventId = id;
        attendance.CreatedAt = DateTime.UtcNow;
        attendance.UpdatedAt = DateTime.UtcNow;

        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();
        return Ok(attendance);
    }
}