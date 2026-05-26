namespace Qut.PartnerForge.Api.Models;

/// <summary>
/// Pre-project proposal submitted by partners; may mature into <see cref="ResultingProject"/> after staff review.
/// </summary>
public class ProjectApplication
{
    public Guid Id { get; set; }
    public Guid OrganisationId { get; set; }
    public Guid? ContactId { get; set; }
    public string ProposedTitle { get; set; } = string.Empty;
    public string? ProposedDescription { get; set; }
    public string? ProposedProjectType { get; set; }
    public string? ProposedSemester { get; set; }
    public int? ProposedYear { get; set; }
    public int? ProposedFacultyId { get; set; }
    public bool ProposedMultipleTeams { get; set; }
    public string? ProposedDisciplineArea { get; set; }
    public string? ProposedSecondaryItDiscipline { get; set; }
    public string? ProposedDeliverables { get; set; }
    public string? StudentProjectAgreement { get; set; }
    public string? IpAssignmentRationale { get; set; }
    public string ApplicationStatus { get; set; } = "pending";
    public Guid? SubmittedBy { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public Guid? ResultingProjectId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Organisation Organisation { get; set; } = null!;
    public Contact? Contact { get; set; }
    public Faculty? ProposedFaculty { get; set; }
    public Project? ResultingProject { get; set; }
}
