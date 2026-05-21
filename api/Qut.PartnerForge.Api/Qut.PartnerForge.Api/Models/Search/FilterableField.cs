namespace Qut.PartnerForge.Api.Models.Search;

/// <summary>
/// Descriptor returned by the fields endpoint so the UI can render appropriate filter controls.
/// </summary>
/// <param name="Field">Snake_case field identifier used in <see cref="FilterItem.Field"/>.</param>
/// <param name="Label">Human-readable label for display.</param>
/// <param name="Type">Data type hint: <c>text</c>, <c>number</c>, <c>select</c>, or <c>date</c>.</param>
/// <param name="Options">Allowed values when <paramref name="Type"/> is <c>select</c>; null otherwise.</param>
public record FilterableField(
    string Field,
    string Label,
    string Type,
    string[]? Options
);
