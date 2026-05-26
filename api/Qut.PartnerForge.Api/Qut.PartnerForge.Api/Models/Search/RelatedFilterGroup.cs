namespace Qut.PartnerForge.Api.Models.Search;

/// <summary>
/// A set of filter predicates applied to a related entity via a navigation collection.
/// Translated to an <c>EXISTS</c> (any) or <c>NOT EXISTS</c> (none) subquery.
/// </summary>
/// <param name="Entity">Related entity key (e.g. <c>projects</c>, <c>events</c>).</param>
/// <param name="Quantifier"><c>any</c> or <c>none</c>.</param>
/// <param name="Filters">Predicates evaluated on the related entity's columns.</param>
public record RelatedFilterGroup(
    string Entity,
    string Quantifier,
    List<FilterItem> Filters
);
