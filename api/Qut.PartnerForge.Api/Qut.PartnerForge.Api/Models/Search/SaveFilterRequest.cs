namespace Qut.PartnerForge.Api.Models.Search;

/// <summary>
/// Payload for persisting a named filter preset against an entity.
/// </summary>
/// <param name="Entity">Target entity (e.g. <c>organisations</c>, <c>projects</c>, <c>events</c>).</param>
/// <param name="Name">User-chosen label for the saved filter set.</param>
/// <param name="Filters">Filter predicates to store.</param>
/// <param name="RelatedFilters">Optional cross-table filter groups to persist alongside direct filters.</param>
public record SaveFilterRequest(
    string Entity,
    string Name,
    List<FilterItem> Filters,
    List<RelatedFilterGroup>? RelatedFilters
);
