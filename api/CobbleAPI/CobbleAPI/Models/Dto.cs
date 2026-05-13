namespace CobbleAPI.Models;

// Auth DTOs

public record LoginRequest(
    string Email,
    string Password
);

public record RegisterUserRequest(
    string Email,
    string Password,
    string FullName,
    string Role,       // "admin" | "course_organiser" | "industry_partner"
    int? FacultyId
);

public record AuthResponse(
    string Token,
    string Email,
    string FullName,
    string Role,
    int? FacultyId,
    Guid? OrganisationId
);

// Read-only list DTOs

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

public record SaveOrganisationRequest(
    string Name,
    int? IndustryId,
    string? Email,
    string? Website,
    string? Phone,
    string PartnershipStatus,
    string? Notes
);

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

public record SaveEventRequest(
    string Name,
    string? Description,
    string? EventType,
    DateOnly EventDate,
    string? Location,
    int? FacultyId,
    string? Notes
);

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

public record SaveUserRequest(
    string Email,
    string? Password,
    string FullName,
    string Role,
    int? FacultyId,
    Guid? OrganisationId,
    bool IsActive
);
