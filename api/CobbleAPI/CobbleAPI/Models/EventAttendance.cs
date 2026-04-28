namespace CobbleAPI.Models;

public class EventAttendance
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid OrganisationId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Event Event { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
}
