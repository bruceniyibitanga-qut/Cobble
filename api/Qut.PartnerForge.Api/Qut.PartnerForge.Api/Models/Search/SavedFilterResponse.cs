namespace Qut.PartnerForge.Api.Models.Search;

/// <summary>
/// Projection returned when listing a user's saved filter presets.
/// </summary>
/// <param name="Id">Saved filter identifier.</param>
/// <param name="Name">User-chosen label.</param>
/// <param name="Entity">Entity the filters apply to.</param>
/// <param name="Filters">JSON-serialized filter array.</param>
/// <param name="CreatedAt">Timestamp when the preset was saved.</param>
public record SavedFilterResponse(
    Guid Id,
    string Name,
    string Entity,
    string Filters,
    DateTime CreatedAt
);
