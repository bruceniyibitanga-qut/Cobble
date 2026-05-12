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
    string PartnershipStatus,
    string SubmissionStatus,
    string? PrimaryContactName,
    string? PrimaryContactEmail,
    int ProjectCount,
    DateTime UpdatedAt
);

public record ProjectListItemDto(
    Guid Id,
    string Title,
    string OrganisationName,
    string? Faculty,
    string ProjectType,
    string Semester,
    int Year,
    string Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateTime UpdatedAt
);

public record ProjectApplicationListItemDto(
    Guid Id,
    string ProposedTitle,
    string OrganisationName,
    string? ContactName,
    string? ProposedProjectType,
    string? ProposedSemester,
    int? ProposedYear,
    string? ProposedFaculty,
    string ApplicationStatus,
    DateTime SubmittedAt
);
