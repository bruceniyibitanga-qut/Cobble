namespace Qut.PartnerForge.Api.Models;

// Auth DTOs

/// <summary>Credentials exchanged for <see cref="AuthResponse"/>.</summary>
/// <param name="Email">User email.</param>
/// <param name="Password">Plain-text password (transport should use HTTPS).</param>
public record LoginRequest(
    string Email,
    string Password
);

/// <summary>
/// Admin-triggered onboarding payload for roles that authenticate against <see cref="User"/>.
/// </summary>
/// <param name="Email">Unique login email.</param>
/// <param name="Password">Initial password hashed server-side.</param>
/// <param name="FullName">Display name.</param>
/// <param name="Role">Logical role name (<c>admin</c>, <c>course_organiser</c>, <c>industry_partner</c>).</param>
/// <param name="FacultyId">Optional faculty for course organisers.</param>
public record RegisterUserRequest(
    string Email,
    string Password,
    string FullName,
    string Role,
    int? FacultyId
);

/// <summary>Successful authentication payload.</summary>
/// <param name="Token">JWT bearer compact string.</param>
/// <param name="Email">Authenticated email claim.</param>
/// <param name="FullName">Human-readable profile name.</param>
/// <param name="Role">Authorisation role copied from <see cref="Role.Name"/>.</param>
/// <param name="FacultyId">Optional faculty routing for scoped queries.</param>
/// <param name="OrganisationId">Optional owning organisation when the user is an industry partner.</param>
public record AuthResponse(
    string Token,
    string Email,
    string FullName,
    string Role,
    int? FacultyId,
    Guid? OrganisationId
);

// Read-only list DTOs

/// <summary>Projection for organisation directory rows.</summary>
public record OrganisationListItemDto(
    Guid Id,
    string Name,
    string? Industry,
    int? IndustryId,
    string? Email,
    string? Website,
    string? Phone,
    string PartnershipStatus,
    string SubmissionStatus,
    string? PrimaryContactName,
    string? PrimaryContactEmail,
    int ProjectCount,
    int PendingApplicationCount,
    DateTime UpdatedAt
);

/// <summary>Payload for organisation create/update controllers.</summary>
public record SaveOrganisationRequest(
    string Name,
    int? IndustryId,
    string? Email,
    string? Website,
    string? Phone,
    string PartnershipStatus,
    string? Notes
);

/// <summary>Projection for listing or viewing projects.</summary>
public record ProjectListItemDto(
    Guid Id,
    string Title,
    string? Description,
    Guid OrganisationId,
    string OrganisationName,
    int? FacultyId,
    string? Faculty,
    string ProjectType,
    string Semester,
    int Year,
    string Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateTime UpdatedAt
);

/// <summary>Payload for creating or replacing project fields.</summary>
public record SaveProjectRequest(
    string Title,
    string? Description,
    Guid OrganisationId,
    int? FacultyId,
    string ProjectType,
    string Semester,
    int Year,
    string Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Notes
);

/// <summary>Partner application row without linked <see cref="Project"/> metadata.</summary>
public record ProjectApplicationListItemDto(
    Guid Id,
    string ProposedTitle,
    string? ProposedDescription,
    Guid OrganisationId,
    string OrganisationName,
    Guid? ContactId,
    string? ContactName,
    string? ProposedProjectType,
    string? ProposedSemester,
    int? ProposedYear,
    string? ProposedFaculty,
    string ApplicationStatus,
    DateTime SubmittedAt
);

/// <summary>Payload describing a prospective project awaiting staff review.</summary>
public record SaveProjectApplicationRequest(
    Guid OrganisationId,
    Guid? ContactId,
    string ProposedTitle,
    string? ProposedDescription,
    string? ProposedProjectType,
    string? ProposedSemester,
    int? ProposedYear,
    int? ProposedFacultyId
);

/// <summary>Projection for calendars and attendee counts.</summary>
public record EventListItemDto(
    Guid Id,
    string Name,
    string? Description,
    string? EventType,
    DateOnly EventDate,
    string? Location,
    int? FacultyId,
    string? Faculty,
    int AttendanceCount,
    DateTime UpdatedAt
);

/// <summary>Payload describing an event surfaced to partners.</summary>
public record SaveEventRequest(
    string Name,
    string? Description,
    string? EventType,
    DateOnly EventDate,
    string? Location,
    int? FacultyId,
    string? Notes
);

/// <summary>Admin directory row for workforce accounts.</summary>
public record UserListItemDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    int? FacultyId,
    string? Faculty,
    Guid? OrganisationId,
    string? OrganisationName,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime UpdatedAt
);

/// <summary>Payload for provisioning or patching <see cref="User"/> profiles.</summary>
public record SaveUserRequest(
    string Email,
    string? Password,
    string FullName,
    string Role,
    int? FacultyId,
    Guid? OrganisationId,
    bool IsActive
);
