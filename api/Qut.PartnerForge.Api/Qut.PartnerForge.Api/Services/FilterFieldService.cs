using Qut.PartnerForge.Api.Interfaces;
using Qut.PartnerForge.Api.Models.Search;

namespace Qut.PartnerForge.Api.Services;

/// <summary>
/// In-memory registry of filterable fields per entity with operator validation by field type.
/// Registered as a singleton since the whitelist is static.
/// </summary>
public class FilterFieldService : IFilterFieldService
{
    private readonly Dictionary<string, List<FilterableField>> _fields;
    private readonly Dictionary<string, string[]> _operatorsByType;
    private readonly Dictionary<string, List<RelationshipDescriptor>> _relationships;

    /// <summary>
    /// Initializes the field whitelist and operator rules.
    /// </summary>
    public FilterFieldService()
    {
        _fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["organisations"] =
            [
                new("registration_id", "Registration ID", "text", null),
                new("name", "Name", "text", null),
                new("abn", "ABN", "text", null),
                new("partnership_status", "Partnership Status", "select",
                    ["prospect", "active", "completed", "inactive"]),
                new("submission_status", "Submission Status", "select",
                    ["pending", "approved", "rejected", "archived"]),
                new("city", "City", "text", null),
                new("state", "State", "text", null),
                new("country", "Country", "text", null),
            ],
            ["projects"] =
            [
                new("title", "Title", "text", null),
                new("project_type", "Project Type", "select",
                    ["capstone", "undergrad", "postgrad"]),
                new("discipline_area", "Discipline Area", "text", null),
                new("semester", "Semester", "select",
                    ["S1", "S2", "SS"]),
                new("year", "Year", "number", null),
                new("status", "Status", "select",
                    ["proposed", "ongoing", "completed", "cancelled"]),
            ],
            ["events"] =
            [
                new("name", "Name", "text", null),
                new("event_type", "Event Type", "text", null),
                new("event_date", "Event Date", "date", null),
                new("location", "Location", "text", null),
            ],
            ["contacts"] =
            [
                new("first_name", "First Name", "text", null),
                new("last_name", "Last Name", "text", null),
                new("email", "Email", "text", null),
                new("job_title", "Job Title", "text", null),
                new("is_primary", "Is Primary", "select", ["true", "false"]),
            ],
            ["project_applications"] =
            [
                new("proposed_title", "Proposed Title", "text", null),
                new("application_status", "Application Status", "select",
                    ["pending", "approved", "rejected", "archived"]),
                new("proposed_project_type", "Proposed Type", "select",
                    ["capstone", "undergrad", "postgrad"]),
                new("proposed_discipline_area", "Proposed Discipline Area", "text", null),
                new("proposed_semester", "Proposed Semester", "select",
                    ["S1", "S2", "SS"]),
                new("proposed_year", "Proposed Year", "number", null),
            ],
        };

        _operatorsByType = new(StringComparer.OrdinalIgnoreCase)
        {
            ["text"] = ["equals", "contains"],
            ["number"] = ["equals", "greaterThan", "lessThan"],
            ["select"] = ["equals"],
            ["date"] = ["equals", "greaterThan", "lessThan"],
        };

        _relationships = new(StringComparer.OrdinalIgnoreCase)
        {
            ["organisations"] =
            [
                new("projects", "Projects"),
                new("events", "Events"),
                new("contacts", "Contacts"),
                new("project_applications", "Project Applications"),
            ],
            ["projects"] =
            [
                new("organisations", "Organisation"),
            ],
            ["events"] =
            [
                new("organisations", "Organisations"),
            ],
        };
    }

    /// <inheritdoc />
    public List<FilterableField> GetFields(string entity) =>
        _fields.TryGetValue(entity, out var fields) ? fields : [];

    /// <inheritdoc />
    public bool IsFieldAllowed(string entity, string field) =>
        _fields.TryGetValue(entity, out var fields) &&
        fields.Exists(f => string.Equals(f.Field, field, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public bool IsOperatorAllowed(string entity, string field, string op)
    {
        if (!_fields.TryGetValue(entity, out var fields))
            return false;

        var descriptor = fields.Find(f =>
            string.Equals(f.Field, field, StringComparison.OrdinalIgnoreCase));

        if (descriptor == null)
            return false;

        return _operatorsByType.TryGetValue(descriptor.Type, out var allowed) &&
               allowed.Contains(op, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public List<RelationshipDescriptor> GetRelationships(string entity) =>
        _relationships.TryGetValue(entity, out var rels) ? rels : [];

    /// <inheritdoc />
    public bool IsRelationshipAllowed(string rootEntity, string relatedEntity) =>
        _relationships.TryGetValue(rootEntity, out var rels) &&
        rels.Exists(r => string.Equals(r.Entity, relatedEntity, StringComparison.OrdinalIgnoreCase));
}
