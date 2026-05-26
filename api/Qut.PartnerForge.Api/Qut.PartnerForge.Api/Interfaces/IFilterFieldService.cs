using Qut.PartnerForge.Api.Models.Search;

namespace Qut.PartnerForge.Api.Interfaces;

/// <summary>
/// Whitelist-based validation of filterable fields and operators per entity.
/// </summary>
public interface IFilterFieldService
{
    /// <summary>
    /// Returns the filterable field descriptors for the given entity, or an empty list if unknown.
    /// </summary>
    List<FilterableField> GetFields(string entity);

    /// <summary>
    /// Checks whether <paramref name="field"/> is in the whitelist for <paramref name="entity"/>.
    /// </summary>
    bool IsFieldAllowed(string entity, string field);

    /// <summary>
    /// Checks whether <paramref name="op"/> is valid for the declared type of <paramref name="field"/>
    /// on <paramref name="entity"/>.
    /// </summary>
    bool IsOperatorAllowed(string entity, string field, string op);

    /// <summary>
    /// Returns the joinable related entities for <paramref name="entity"/>, or an empty list if unknown.
    /// </summary>
    List<RelationshipDescriptor> GetRelationships(string entity);

    /// <summary>
    /// Checks whether <paramref name="relatedEntity"/> can be joined from <paramref name="rootEntity"/>.
    /// </summary>
    bool IsRelationshipAllowed(string rootEntity, string relatedEntity);
}
