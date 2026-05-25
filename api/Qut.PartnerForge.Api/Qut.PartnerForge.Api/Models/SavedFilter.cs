namespace Qut.PartnerForge.Api.Models;

/// <summary>
/// User-owned named filter preset persisted as a JSON array of <see cref="Search.FilterItem"/> predicates.
/// </summary>
public class SavedFilter
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Filters { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
