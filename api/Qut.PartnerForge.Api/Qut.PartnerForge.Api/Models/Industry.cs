namespace Qut.PartnerForge.Api.Models;

/// <summary>Industry vertical used to classify organisations.</summary>
public class Industry
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Organisation> Organisations { get; set; } = new List<Organisation>();
}
