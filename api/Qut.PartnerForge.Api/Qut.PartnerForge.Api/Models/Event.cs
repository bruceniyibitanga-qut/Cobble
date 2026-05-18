namespace Qut.PartnerForge.Api.Models;

/// <summary>
/// Recruitment or informational calendar entry optionally scoped by <see cref="Faculty"/>.
/// </summary>
public class Event
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EventType { get; set; }
    public DateOnly EventDate { get; set; }
    public string? Location { get; set; }
    public int? FacultyId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Faculty? Faculty { get; set; }
    public ICollection<EventAttendance> Attendances { get; set; } = new List<EventAttendance>();
}
