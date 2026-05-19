namespace Qut.PartnerForge.Api.Models;

/// <summary>
/// JSON snapshot of mutated rows (<c>old_values</c>/<c>new_values</c>) attributable to authenticated actions.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
