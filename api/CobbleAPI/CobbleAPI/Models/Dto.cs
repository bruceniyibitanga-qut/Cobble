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
