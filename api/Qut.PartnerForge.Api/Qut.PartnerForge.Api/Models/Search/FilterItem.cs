namespace Qut.PartnerForge.Api.Models.Search;

/// <summary>
/// Single filter predicate specifying a field, comparison operator, and target value.
/// </summary>
/// <param name="Field">Snake_case column name (must be in the entity whitelist).</param>
/// <param name="Operator">Comparison verb: <c>equals</c>, <c>contains</c>, <c>greaterThan</c>, <c>lessThan</c>.</param>
/// <param name="Value">String-encoded value converted server-side to the property CLR type.</param>
public record FilterItem(
    string Field,
    string Operator,
    string Value
);
