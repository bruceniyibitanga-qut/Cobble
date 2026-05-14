namespace CobbleAPI.Models;

/// <summary>
/// Academic organisational unit aligning staff, placements, and events.
/// </summary>
public class Faculty
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
