namespace Qut.PartnerForge.Api.Models;

/// <summary>
/// Individual associated with an <see cref="Organisation"/>; may be nominated on applications.
/// </summary>
public class Contact
{
    public Guid Id { get; set; }
    public Guid OrganisationId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsPrimary { get; set; } = false;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Organisation Organisation { get; set; } = null!;
    public ICollection<ProjectApplication> ProjectApplications { get; set; } = new List<ProjectApplication>();
}
