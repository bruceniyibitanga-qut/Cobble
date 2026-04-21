namespace CobbleAPI.Models;

// Auth DTOs 

public record RegisterOrganisationRequest(
    string TenantName,
    string AdminEmail,
    string AdminPassword,
    string AdminFullName
);

public record JoinTenantRequest(
    Guid TenantId,
    string Email,
    string Password,
    string FullName
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    string Email,
    string FullName,
    string Role,
    Guid TenantId,
    string TenantName
);

// Tenant DTOs

public record TenantResponse(
    Guid Id,
    string Name,
    int UserCount,
    int AdminCount,
    DateTime CreatedAt
);