using CobbleAPI.Data;
using CobbleAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    // 获取所有申请
    [HttpGet]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<ActionResult<IEnumerable<ProjectApplication>>> GetAll(
        [FromQuery] string? status)
    {
        var query = _context.ProjectApplications
            .Where(a => a.DeletedAt == null)
            .Include(a => a.Organisation)
            .Include(a => a.Contact)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.ApplicationStatus == status);

        return Ok(await query.ToListAsync());
    }

    // 获取单个申请
    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectApplication>> GetById(Guid id)
    {
        var app = await _context.ProjectApplications
            .Include(a => a.Organisation)
            .Include(a => a.Contact)
            .Include(a => a.ResultingProject)
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

        if (app == null) return NotFound();
        return Ok(app);
    }

    // 企业提交申请
    [HttpPost]
    public async Task<ActionResult<ProjectApplication>> Create(ProjectApplication app)
    {
        app.Id = Guid.NewGuid();
        app.ApplicationStatus = "pending";
        app.SubmittedAt = DateTime.UtcNow;
        app.CreatedAt = DateTime.UtcNow;
        app.UpdatedAt = DateTime.UtcNow;

        _context.ProjectApplications.Add(app);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = app.Id }, app);
    }

    // 管理员审批申请
    [HttpPut("{id}/review")]
    [Authorize(Roles = "admin,course_organiser")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewRequest request)
    {
        var app = await _context.ProjectApplications
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

        if (app == null) return NotFound();

        app.ApplicationStatus = request.Status; // "approved" 或 "rejected"
        app.ReviewNotes = request.Notes;
        app.ReviewedAt = DateTime.UtcNow;
        app.UpdatedAt = DateTime.UtcNow;

        // 如果审批通过，自动创建项目
        if (request.Status == "approved")
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = app.ProposedTitle,
                Description = app.ProposedDescription,
                OrganisationId = app.OrganisationId,
                FacultyId = app.ProposedFacultyId,
                ProjectType = app.ProposedProjectType ?? "capstone",
                Semester = app.ProposedSemester ?? "S1",
                Year = app.ProposedYear ?? DateTime.UtcNow.Year,
                Status = "proposed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            app.ResultingProjectId = project.Id;
        }

        await _context.SaveChangesAsync();
        return Ok(app);
    }

    // 删除申请（软删除）
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var app = await _context.ProjectApplications
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

        if (app == null) return NotFound();

        app.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

// 审批请求的数据格式
public class ReviewRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}