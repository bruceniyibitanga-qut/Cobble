namespace Qut.PartnerForge.Api.Models;

/// <summary>
/// Partner RSVP record pairing an organisation with <see cref="Event"/>.
/// </summary>
public class EventAttendance
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid OrganisationId { get; set; }
    public string? FirstName { get; set; }
    public string? BestContactName { get; set; }
    public string? PositionTitle { get; set; }
    public string? Email { get; set; }
    public string? ListName { get; set; }
    public string? Source { get; set; }
    public string? EventsInvitedTo { get; set; }
    public string? Response { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Event Event { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
}
