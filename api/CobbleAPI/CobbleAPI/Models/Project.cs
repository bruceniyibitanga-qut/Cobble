namespace CobbleAPI.Models;

public class Project
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OrganisationId { get; set; }
    public int? FacultyId { get; set; }
    public string ProjectType { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Status { get; set; } = "ongoing";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Organisation? Organisation { get; set; }
    public Faculty? Faculty { get; set; }
    public ICollection<ProjectApplication> Applications { get; set; } = new List<ProjectApplication>();
}
