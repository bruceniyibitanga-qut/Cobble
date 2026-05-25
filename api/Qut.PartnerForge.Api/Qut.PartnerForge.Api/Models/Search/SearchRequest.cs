namespace Qut.PartnerForge.Api.Models.Search;

/// <summary>
/// Paginated search payload with dynamic filters and optional sort.
/// </summary>
/// <param name="Filters">Zero or more filter predicates ANDed together.</param>
/// <param name="SortBy">Optional snake_case field name to order by (must be in the whitelist).</param>
/// <param name="SortDirection"><c>asc</c> or <c>desc</c>.</param>
/// <param name="Page">One-based page index.</param>
/// <param name="PageSize">Rows per page (clamped server-side).</param>
/// <param name="RelatedFilters">Optional cross-table filter groups joined via navigation collections.</param>
public record SearchRequest(
    List<FilterItem> Filters,
    List<RelatedFilterGroup>? RelatedFilters,
    string? SortBy,
    string SortDirection = "asc",
    int Page = 1,
    int PageSize = 20
);
