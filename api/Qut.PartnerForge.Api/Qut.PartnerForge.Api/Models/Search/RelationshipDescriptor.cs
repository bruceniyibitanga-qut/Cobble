namespace Qut.PartnerForge.Api.Models.Search;

/// <summary>
/// Describes a joinable related entity that can be used in cross-table filter conditions.
/// </summary>
/// <param name="Entity">Related entity key matching the field-whitelist key (e.g. <c>projects</c>).</param>
/// <param name="Label">Human-readable label for UI display.</param>
public record RelationshipDescriptor(
    string Entity,
    string Label
);
