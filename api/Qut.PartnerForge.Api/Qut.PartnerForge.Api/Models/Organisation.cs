namespace Qut.PartnerForge.Api.Models;

/// <summary>
/// Industry organisation profile with partnership funnel fields, geography, reviewers, and child collections for contacts and engagements.
/// </summary>
public class Organisation
{
    public Guid Id { get; set; }
    public string? RegistrationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Abn { get; set; }
    public int? IndustryId { get; set; }
    public string? Website { get; set; }
    public string? Domain { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Postcode { get; set; }
    public string Country { get; set; } = "Australia";
    public string? OrganisationInformation { get; set; }
    public string? Notes { get; set; }
    public string PartnershipStatus { get; set; } = "prospect";
    public string SubmissionStatus { get; set; } = "approved";
    public Guid? SubmittedBy { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Industry? Industry { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<EventAttendance> EventAttendances { get; set; } = new List<EventAttendance>();
    public ICollection<ProjectApplication> ProjectApplications { get; set; } = new List<ProjectApplication>();
}
