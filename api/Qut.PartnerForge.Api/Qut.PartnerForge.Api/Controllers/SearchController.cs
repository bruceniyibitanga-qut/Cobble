using Qut.PartnerForge.Api.Data;
using Qut.PartnerForge.Api.Interfaces;
using Qut.PartnerForge.Api.Models;
using Qut.PartnerForge.Api.Models.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;

namespace Qut.PartnerForge.Api.Controllers;

/// <summary>
/// Generic search, filter, and saved-filter endpoints that work across multiple entity types
/// using expression-tree-based dynamic filtering with whitelist validation.
/// </summary>
[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFilterFieldService _fieldService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Maps each supported entity to a dictionary of snake_case field -> PascalCase property name.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, string>> FieldPropertyMaps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["organisations"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["registration_id"] = nameof(Organisation.RegistrationId),
            ["name"] = nameof(Organisation.Name),
            ["abn"] = nameof(Organisation.Abn),
            ["partnership_status"] = nameof(Organisation.PartnershipStatus),
            ["submission_status"] = nameof(Organisation.SubmissionStatus),
            ["city"] = nameof(Organisation.City),
            ["state"] = nameof(Organisation.State),
            ["country"] = nameof(Organisation.Country),
        },
        ["projects"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["title"] = nameof(Project.Title),
            ["project_type"] = nameof(Project.ProjectType),
            ["discipline_area"] = nameof(Project.DisciplineArea),
            ["semester"] = nameof(Project.Semester),
            ["year"] = nameof(Project.Year),
            ["status"] = nameof(Project.Status),
        },
        ["events"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = nameof(Event.Name),
            ["event_type"] = nameof(Event.EventType),
            ["event_date"] = nameof(Event.EventDate),
            ["location"] = nameof(Event.Location),
        },
        ["contacts"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["first_name"] = nameof(Contact.FirstName),
            ["last_name"] = nameof(Contact.LastName),
            ["email"] = nameof(Contact.Email),
            ["job_title"] = nameof(Contact.JobTitle),
            ["is_primary"] = nameof(Contact.IsPrimary),
        },
        ["project_applications"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["proposed_title"] = nameof(ProjectApplication.ProposedTitle),
            ["application_status"] = nameof(ProjectApplication.ApplicationStatus),
            ["proposed_project_type"] = nameof(ProjectApplication.ProposedProjectType),
            ["proposed_discipline_area"] = nameof(ProjectApplication.ProposedDisciplineArea),
            ["proposed_semester"] = nameof(ProjectApplication.ProposedSemester),
            ["proposed_year"] = nameof(ProjectApplication.ProposedYear),
        },
    };

    /// <summary>
    /// Metadata describing how to traverse from a root entity to a related entity's navigation collection.
    /// </summary>
    /// <param name="CollectionProperty">Navigation property name on the root type (e.g. <c>Projects</c>).</param>
    /// <param name="CollectionElementType">CLR type of each element in the collection.</param>
    /// <param name="HopProperty">If non-null, filter properties are resolved on this inner navigation rather than on the collection element itself (two-hop join).</param>
    /// <param name="HopType">CLR type of the hop target, or null for direct joins.</param>
    private record NavigationInfo(string CollectionProperty, Type CollectionElementType, string? HopProperty, Type? HopType);

    private static readonly Dictionary<(string Root, string Related), NavigationInfo> NavigationMap = new()
    {
        [("organisations", "projects")] = new(nameof(Organisation.Projects), typeof(Project), null, null),
        [("organisations", "contacts")] = new(nameof(Organisation.Contacts), typeof(Contact), null, null),
        [("organisations", "project_applications")] = new(nameof(Organisation.ProjectApplications), typeof(ProjectApplication), null, null),
        [("organisations", "events")] = new(nameof(Organisation.EventAttendances), typeof(EventAttendance), nameof(EventAttendance.Event), typeof(Event)),
        [("projects", "organisations")] = new(nameof(Project.Organisation), typeof(Organisation), null, null),
        [("events", "organisations")] = new(nameof(Event.Attendances), typeof(EventAttendance), nameof(EventAttendance.Organisation), typeof(Organisation)),
    };

    /// <summary>
    /// Initializes a new instance of <see cref="SearchController"/>.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="fieldService">Whitelist validation service.</param>
    public SearchController(ApplicationDbContext context, IFilterFieldService fieldService)
    {
        _context = context;
        _fieldService = fieldService;
    }

    // ──────────────────────────────────────────────
    //  Field metadata
    // ──────────────────────────────────────────────

    /// <summary>
    /// Returns the filterable field descriptors for an entity so the UI can render appropriate controls.
    /// </summary>
    /// <param name="entity">Entity name (<c>organisations</c>, <c>projects</c>, <c>events</c>).</param>
    [HttpGet("fields/{entity}")]
    public ActionResult<List<FilterableField>> GetFields(string entity)
    {
        var fields = _fieldService.GetFields(entity);
        if (fields.Count == 0)
            return NotFound(new { message = $"Unknown entity '{entity}'." });

        return Ok(fields);
    }

    // ──────────────────────────────────────────────
    //  Relationship metadata
    // ──────────────────────────────────────────────

    /// <summary>
    /// Returns the joinable related entities for a root entity so the UI can offer cross-table conditions.
    /// </summary>
    /// <param name="entity">Root entity name.</param>
    [HttpGet("relationships/{entity}")]
    public ActionResult<List<RelationshipDescriptor>> GetRelationships(string entity)
    {
        var rels = _fieldService.GetRelationships(entity);
        return Ok(rels);
    }

    // ──────────────────────────────────────────────
    //  Dynamic search
    // ──────────────────────────────────────────────

    /// <summary>
    /// Executes a paginated, filtered, and sorted query against the specified entity.
    /// Every filter field and operator is validated against the whitelist before the query is built.
    /// </summary>
    /// <param name="entity">Entity name.</param>
    /// <param name="request">Search payload with filters, sort, and pagination.</param>
    [HttpPost("{entity}")]
    public async Task<IActionResult> Search(string entity, [FromBody] SearchRequest request)
    {
        if (!FieldPropertyMaps.ContainsKey(entity))
            return NotFound(new { message = $"Unknown entity '{entity}'." });

        var validationError = ValidateFilters(entity, request.Filters);
        if (validationError != null)
            return BadRequest(new { message = validationError });

        var relatedError = ValidateRelatedFilters(entity, request.RelatedFilters);
        if (relatedError != null)
            return BadRequest(new { message = relatedError });

        if (request.SortBy != null && !_fieldService.IsFieldAllowed(entity, request.SortBy))
            return BadRequest(new { message = $"Sort field '{request.SortBy}' is not allowed for '{entity}'." });

        return entity.ToLowerInvariant() switch
        {
            "organisations" => await ExecuteSearch(
                _context.Organisations,
                entity,
                request,
                o => new OrganisationListItemDto(
                    o.Id,
                    o.RegistrationId,
                    o.Name,
                    o.Abn,
                    o.Industry != null ? o.Industry.Name : null,
                    o.IndustryId,
                    o.Email,
                    o.Website,
                    o.Phone,
                    o.AddressLine1,
                    o.AddressLine2,
                    o.City,
                    o.State,
                    o.Postcode,
                    o.Country,
                    o.OrganisationInformation,
                    o.PartnershipStatus,
                    o.SubmissionStatus,
                    o.Contacts
                        .Where(c => c.DeletedAt == null && c.IsPrimary)
                        .Select(c => c.FirstName + " " + c.LastName)
                        .FirstOrDefault(),
                    o.Contacts
                        .Where(c => c.DeletedAt == null && c.IsPrimary)
                        .Select(c => c.JobTitle)
                        .FirstOrDefault(),
                    o.Contacts
                        .Where(c => c.DeletedAt == null && c.IsPrimary)
                        .Select(c => c.Email)
                        .FirstOrDefault(),
                    o.Contacts
                        .Where(c => c.DeletedAt == null && c.IsPrimary)
                        .Select(c => c.Phone)
                        .FirstOrDefault(),
                    o.Projects.Count(p => p.DeletedAt == null),
                    o.ProjectApplications.Count(a =>
                        a.DeletedAt == null &&
                        a.ApplicationStatus == "pending" &&
                        a.ResultingProjectId == null),
                    o.UpdatedAt
                )),
            "projects" => await ExecuteSearch(
                _context.Projects,
                entity,
                request,
                p => new ProjectListItemDto(
                    p.Id,
                    p.Title,
                    p.Description,
                    p.OrganisationId,
                    p.Organisation.Name,
                    p.FacultyId,
                    p.Faculty != null ? p.Faculty.Name : null,
                    p.ProjectType,
                    p.Semester,
                    p.Year,
                    p.Status,
                    p.StartDate,
                    p.EndDate,
                    p.MultipleTeams,
                    p.DisciplineArea,
                    p.SecondaryItDiscipline,
                    p.ProjectDeliverables,
                    p.ProjectPartnerAgreement,
                    p.StudentProjectAgreement,
                    p.IpAssignmentRationale,
                    p.UpdatedAt
                )),
            "events" => await ExecuteSearch(
                _context.Events,
                entity,
                request,
                e => new EventListItemDto(
                    e.Id,
                    e.Name,
                    e.Description,
                    e.EventType,
                    e.EventDate,
                    e.Location,
                    e.FacultyId,
                    e.Faculty != null ? e.Faculty.Name : null,
                    e.Attendances.Count(a => a.DeletedAt == null),
                    e.UpdatedAt
                )),
            _ => NotFound(new { message = $"Unknown entity '{entity}'." }),
        };
    }

    // ──────────────────────────────────────────────
    //  Saved filters
    // ──────────────────────────────────────────────

    /// <summary>
    /// Lists the current user's saved filter presets for a given entity.
    /// </summary>
    /// <param name="entity">Entity name.</param>
    [HttpGet("saved-filters/{entity}")]
    public async Task<ActionResult<List<SavedFilterResponse>>> GetSavedFilters(string entity)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var filters = await _context.SavedFilters
            .AsNoTracking()
            .Where(sf => sf.UserId == userId.Value && sf.Entity == entity)
            .OrderByDescending(sf => sf.CreatedAt)
            .Select(sf => new SavedFilterResponse(sf.Id, sf.Name, sf.Entity, sf.Filters, sf.CreatedAt))
            .ToListAsync();

        return Ok(filters);
    }

    /// <summary>
    /// Persists a named filter set after validating all fields against the whitelist.
    /// </summary>
    /// <param name="request">Filter preset to save.</param>
    [HttpPost("saved-filters")]
    public async Task<ActionResult<SavedFilterResponse>> CreateSavedFilter([FromBody] SaveFilterRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (!FieldPropertyMaps.ContainsKey(request.Entity))
            return BadRequest(new { message = $"Unknown entity '{request.Entity}'." });

        var validationError = ValidateFilters(request.Entity, request.Filters);
        if (validationError != null)
            return BadRequest(new { message = validationError });

        var relatedError = ValidateRelatedFilters(request.Entity, request.RelatedFilters);
        if (relatedError != null)
            return BadRequest(new { message = relatedError });

        var filtersPayload = new
        {
            filters = request.Filters,
            relatedFilters = request.RelatedFilters ?? [],
        };

        var saved = new SavedFilter
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Entity = request.Entity,
            Name = request.Name,
            Filters = JsonSerializer.Serialize(filtersPayload, JsonOptions),
            CreatedAt = DateTime.UtcNow,
        };

        _context.SavedFilters.Add(saved);
        await _context.SaveChangesAsync();

        return Ok(new SavedFilterResponse(saved.Id, saved.Name, saved.Entity, saved.Filters, saved.CreatedAt));
    }

    /// <summary>
    /// Deletes a saved filter preset owned by the current user.
    /// </summary>
    /// <param name="id">Saved filter identifier.</param>
    [HttpDelete("saved-filters/{id}")]
    public async Task<IActionResult> DeleteSavedFilter(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var saved = await _context.SavedFilters.FindAsync(id);
        if (saved == null) return NotFound();

        if (saved.UserId != userId.Value)
            return Forbid();

        _context.SavedFilters.Remove(saved);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ──────────────────────────────────────────────
    //  Private helpers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Parses the current user identifier from the JWT <c>sub</c> claim
    /// (mapped to <see cref="ClaimTypes.NameIdentifier"/> by the middleware).
    /// </summary>
    private Guid? GetCurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    /// <summary>
    /// Returns a validation error message if any filter is disallowed, or null when all pass.
    /// </summary>
    private string? ValidateFilters(string entity, List<FilterItem> filters)
    {
        foreach (var filter in filters)
        {
            if (!_fieldService.IsFieldAllowed(entity, filter.Field))
                return $"Field '{filter.Field}' is not a valid filter for '{entity}'.";

            if (!_fieldService.IsOperatorAllowed(entity, filter.Field, filter.Operator))
                return $"Operator '{filter.Operator}' is not valid for field '{filter.Field}' on '{entity}'.";
        }
        return null;
    }

    /// <summary>
    /// Validates every related filter group: relationship whitelist, field whitelist, operators, and quantifier.
    /// </summary>
    private string? ValidateRelatedFilters(string rootEntity, List<RelatedFilterGroup>? groups)
    {
        if (groups is not { Count: > 0 }) return null;

        foreach (var group in groups)
        {
            if (!_fieldService.IsRelationshipAllowed(rootEntity, group.Entity))
                return $"Relationship from '{rootEntity}' to '{group.Entity}' is not allowed.";

            var q = group.Quantifier.ToLowerInvariant();
            if (q is not ("any" or "none"))
                return $"Quantifier must be 'any' or 'none', got '{group.Quantifier}'.";

            var fieldError = ValidateFilters(group.Entity, group.Filters);
            if (fieldError != null)
                return $"Related '{group.Entity}': {fieldError}";
        }
        return null;
    }

    /// <summary>
    /// Builds and executes a filtered, sorted, paginated query for entity <typeparamref name="T"/>
    /// which must expose a <c>DeletedAt</c> property for soft-delete exclusion.
    /// </summary>
    private async Task<IActionResult> ExecuteSearch<T, TResult>(
        DbSet<T> dbSet,
        string entity,
        SearchRequest request,
        Expression<Func<T, TResult>> selector) where T : class
    {
        var propMap = FieldPropertyMaps[entity];
        IQueryable<T> query = dbSet.AsNoTracking();

        // Soft-delete exclusion
        var param = Expression.Parameter(typeof(T), "e");
        var deletedAtProp = typeof(T).GetProperty("DeletedAt")!;
        var deletedAtAccess = Expression.Property(param, deletedAtProp);
        var nullConst = Expression.Constant(null, deletedAtProp.PropertyType);
        var softDeleteExpr = Expression.Lambda<Func<T, bool>>(
            Expression.Equal(deletedAtAccess, nullConst), param);
        query = query.Where(softDeleteExpr);

        // Dynamic filters
        if (request.Filters.Count > 0)
        {
            var filterExpr = BuildFilterExpression<T>(request.Filters, propMap, param);
            query = query.Where(filterExpr);
        }

        // Related (cross-table) filters
        if (request.RelatedFilters is { Count: > 0 })
        {
            var relatedExpr = BuildRelatedFilterExpressions<T>(entity, request.RelatedFilters, param);
            if (relatedExpr != null)
                query = query.Where(relatedExpr);
        }

        // Total before pagination
        var total = await query.CountAsync();

        // Sorting
        if (!string.IsNullOrWhiteSpace(request.SortBy) && propMap.TryGetValue(request.SortBy, out var sortPropName))
        {
            query = ApplySorting(query, sortPropName, request.SortDirection, param);
        }

        // Pagination
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync();

        return Ok(new { data, total, page, pageSize });
    }

    /// <summary>
    /// Builds a combined <c>Expression&lt;Func&lt;T, bool&gt;&gt;</c> from all filter items ANDed together.
    /// </summary>
    private static Expression<Func<T, bool>> BuildFilterExpression<T>(
        List<FilterItem> filters,
        Dictionary<string, string> propMap,
        ParameterExpression param)
    {
        Expression? combined = null;

        foreach (var filter in filters)
        {
            if (!propMap.TryGetValue(filter.Field, out var propName))
                continue;

            var property = typeof(T).GetProperty(propName);
            if (property == null) continue;

            var propAccess = Expression.Property(param, property);
            var propType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;

            Expression predicate;
            var op = filter.Operator.ToLowerInvariant();

            if (op == "contains")
            {
                // Null-safe string contains: (property ?? "").Contains(value)
                var coalesced = propType == typeof(string)
                    ? (Expression)Expression.Coalesce(propAccess, Expression.Constant(string.Empty))
                    : propAccess;

                var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
                predicate = Expression.Call(coalesced, containsMethod, Expression.Constant(filter.Value));
            }
            else
            {
                var convertedValue = ConvertFilterValue(filter.Value, underlyingType);
                Expression valueExpr = Expression.Constant(convertedValue, propType.IsValueType && Nullable.GetUnderlyingType(propType) != null
                    ? propType
                    : underlyingType);

                // For nullable value types, cast the constant to the nullable wrapper
                if (propType.IsValueType && Nullable.GetUnderlyingType(propType) != null)
                    valueExpr = Expression.Constant(convertedValue, propType);

                Expression accessForCompare = propAccess;
                // For nullable types, use .Value for comparisons (but check HasValue first via short-circuit below if needed)
                // Simpler: just cast both sides — EF Core handles nullable comparison in SQL
                if (Nullable.GetUnderlyingType(propType) != null && op != "equals")
                {
                    accessForCompare = Expression.Convert(propAccess, underlyingType);
                    valueExpr = Expression.Constant(convertedValue, underlyingType);
                }

                predicate = op switch
                {
                    "equals" => Expression.Equal(propAccess, valueExpr),
                    "greaterthan" => Expression.GreaterThan(accessForCompare, valueExpr),
                    "lessthan" => Expression.LessThan(accessForCompare, valueExpr),
                    _ => throw new InvalidOperationException($"Unknown operator '{filter.Operator}'.")
                };
            }

            combined = combined == null ? predicate : Expression.AndAlso(combined, predicate);
        }

        combined ??= Expression.Constant(true);
        return Expression.Lambda<Func<T, bool>>(combined, param);
    }

    /// <summary>
    /// Converts the string filter value to the target CLR type.
    /// </summary>
    private static object ConvertFilterValue(string value, Type targetType) => targetType switch
    {
        _ when targetType == typeof(string) => value,
        _ when targetType == typeof(int) => int.Parse(value),
        _ when targetType == typeof(DateTime) => DateTime.SpecifyKind(DateTime.Parse(value), DateTimeKind.Utc),
        _ when targetType == typeof(DateOnly) => DateOnly.Parse(value),
        _ when targetType == typeof(Guid) => Guid.Parse(value),
        _ when targetType == typeof(bool) => bool.Parse(value),
        _ => Convert.ChangeType(value, targetType),
    };

    /// <summary>
    /// Applies <c>OrderBy</c> / <c>OrderByDescending</c> using an expression tree so EF Core
    /// translates it to SQL rather than evaluating client-side.
    /// </summary>
    private static IQueryable<T> ApplySorting<T>(
        IQueryable<T> query,
        string propertyName,
        string direction,
        ParameterExpression param)
    {
        var property = typeof(T).GetProperty(propertyName);
        if (property == null) return query;

        var propAccess = Expression.Property(param, property);
        var keySelector = Expression.Lambda(propAccess, param);

        var methodName = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase)
            ? "OrderByDescending"
            : "OrderBy";

        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.PropertyType);

        return (IQueryable<T>)method.Invoke(null, [query, keySelector])!;
    }

    /// <summary>
    /// Builds a combined expression from all related-filter groups ANDed together.
    /// Each group becomes <c>.Any(inner => predicate)</c> on the navigation collection,
    /// optionally negated when quantifier is <c>none</c>.
    /// Returns null when there are no groups.
    /// </summary>
    private static Expression<Func<T, bool>>? BuildRelatedFilterExpressions<T>(
        string rootEntity,
        List<RelatedFilterGroup> groups,
        ParameterExpression rootParam) where T : class
    {
        Expression? combined = null;

        foreach (var group in groups)
        {
            var key = (rootEntity.ToLowerInvariant(), group.Entity.ToLowerInvariant());
            if (!NavigationMap.TryGetValue(key, out var nav))
                continue;

            var relatedEntity = group.Entity.ToLowerInvariant();
            if (!FieldPropertyMaps.TryGetValue(relatedEntity, out var relatedPropMap))
            {
                // Two-hop: resolve the field map from the hop target entity
                if (nav.HopType != null)
                {
                    var hopEntityKey = GetEntityKeyForType(nav.HopType);
                    if (hopEntityKey != null)
                        FieldPropertyMaps.TryGetValue(hopEntityKey, out relatedPropMap);
                }
                if (relatedPropMap == null) continue;
            }

            var collectionProp = typeof(T).GetProperty(nav.CollectionProperty);
            if (collectionProp == null) continue;

            // Check if this is a single navigation (e.g. Project.Organisation) rather than a collection
            var isSingleNavigation = !typeof(System.Collections.IEnumerable).IsAssignableFrom(collectionProp.PropertyType)
                                     || collectionProp.PropertyType == typeof(string);

            if (isSingleNavigation)
            {
                var singleExpr = BuildSingleNavigationPredicate(
                    rootParam, collectionProp, nav.CollectionElementType,
                    group.Filters, relatedPropMap);
                if (singleExpr == null) continue;

                Expression predicate = singleExpr;
                if (group.Quantifier.Equals("none", StringComparison.OrdinalIgnoreCase))
                    predicate = Expression.Not(predicate);

                combined = combined == null ? predicate : Expression.AndAlso(combined, predicate);
            }
            else
            {
                var innerParam = Expression.Parameter(nav.CollectionElementType, "r");
                Expression innerBody;

                if (nav.HopProperty != null && nav.HopType != null)
                {
                    innerBody = BuildTwoHopInnerPredicate(
                        innerParam, nav.HopProperty, nav.HopType,
                        group.Filters, relatedPropMap, nav.CollectionElementType);
                }
                else
                {
                    innerBody = BuildInnerPredicate(
                        innerParam, nav.CollectionElementType,
                        group.Filters, relatedPropMap);
                }

                var innerLambda = Expression.Lambda(
                    typeof(Func<,>).MakeGenericType(nav.CollectionElementType, typeof(bool)),
                    innerBody, innerParam);

                var collectionAccess = Expression.Property(rootParam, collectionProp);

                var anyMethod = typeof(Enumerable)
                    .GetMethods()
                    .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                    .MakeGenericMethod(nav.CollectionElementType);

                Expression anyCall = Expression.Call(null, anyMethod, collectionAccess, innerLambda);

                if (group.Quantifier.Equals("none", StringComparison.OrdinalIgnoreCase))
                    anyCall = Expression.Not(anyCall);

                combined = combined == null ? anyCall : Expression.AndAlso(combined, anyCall);
            }
        }

        if (combined == null) return null;
        return Expression.Lambda<Func<T, bool>>(combined, rootParam);
    }

    /// <summary>
    /// Builds the inner predicate body for a direct collection navigation,
    /// including soft-delete exclusion on the related entity.
    /// </summary>
    private static Expression BuildInnerPredicate(
        ParameterExpression innerParam,
        Type elementType,
        List<FilterItem> filters,
        Dictionary<string, string> propMap)
    {
        Expression? body = null;

        // Soft-delete: r.DeletedAt == null
        var deletedAt = elementType.GetProperty("DeletedAt");
        if (deletedAt != null)
        {
            var deletedAccess = Expression.Property(innerParam, deletedAt);
            var nullConst = Expression.Constant(null, deletedAt.PropertyType);
            body = Expression.Equal(deletedAccess, nullConst);
        }

        foreach (var filter in filters)
        {
            if (!propMap.TryGetValue(filter.Field, out var propName)) continue;
            var prop = elementType.GetProperty(propName);
            if (prop == null) continue;

            var predicate = BuildPropertyPredicate(Expression.Property(innerParam, prop), prop.PropertyType, filter);
            body = body == null ? predicate : Expression.AndAlso(body, predicate);
        }

        return body ?? Expression.Constant(true);
    }

    /// <summary>
    /// Builds the inner predicate body for a two-hop navigation (e.g. EventAttendance -> Event),
    /// soft-delete checking both the join entity and the hop target.
    /// </summary>
    private static Expression BuildTwoHopInnerPredicate(
        ParameterExpression innerParam,
        string hopProperty,
        Type hopType,
        List<FilterItem> filters,
        Dictionary<string, string> propMap,
        Type joinEntityType)
    {
        Expression? body = null;

        // Soft-delete on the join entity: ea.DeletedAt == null
        var joinDeletedAt = joinEntityType.GetProperty("DeletedAt");
        if (joinDeletedAt != null)
        {
            var deletedAccess = Expression.Property(innerParam, joinDeletedAt);
            body = Expression.Equal(deletedAccess, Expression.Constant(null, joinDeletedAt.PropertyType));
        }

        // Navigate to the hop target: ea.Event
        var hopNav = joinEntityType.GetProperty(hopProperty);
        if (hopNav == null) return body ?? Expression.Constant(true);
        var hopAccess = Expression.Property(innerParam, hopNav);

        // Soft-delete on the hop target: ea.Event.DeletedAt == null
        var hopDeletedAt = hopType.GetProperty("DeletedAt");
        if (hopDeletedAt != null)
        {
            var hopDeletedAccess = Expression.Property(hopAccess, hopDeletedAt);
            var condition = Expression.Equal(hopDeletedAccess, Expression.Constant(null, hopDeletedAt.PropertyType));
            body = body == null ? condition : Expression.AndAlso(body, condition);
        }

        foreach (var filter in filters)
        {
            if (!propMap.TryGetValue(filter.Field, out var propName)) continue;
            var prop = hopType.GetProperty(propName);
            if (prop == null) continue;

            var predicate = BuildPropertyPredicate(Expression.Property(hopAccess, prop), prop.PropertyType, filter);
            body = body == null ? predicate : Expression.AndAlso(body, predicate);
        }

        return body ?? Expression.Constant(true);
    }

    /// <summary>
    /// Builds filter predicates for a single (non-collection) navigation property,
    /// e.g. <c>Project.Organisation</c>.
    /// </summary>
    private static Expression? BuildSingleNavigationPredicate(
        ParameterExpression rootParam,
        System.Reflection.PropertyInfo navProp,
        Type relatedType,
        List<FilterItem> filters,
        Dictionary<string, string> propMap)
    {
        var navAccess = Expression.Property(rootParam, navProp);
        Expression? body = null;

        // Soft-delete: nav.DeletedAt == null
        var deletedAt = relatedType.GetProperty("DeletedAt");
        if (deletedAt != null)
        {
            var deletedAccess = Expression.Property(navAccess, deletedAt);
            body = Expression.Equal(deletedAccess, Expression.Constant(null, deletedAt.PropertyType));
        }

        foreach (var filter in filters)
        {
            if (!propMap.TryGetValue(filter.Field, out var propName)) continue;
            var prop = relatedType.GetProperty(propName);
            if (prop == null) continue;

            var predicate = BuildPropertyPredicate(Expression.Property(navAccess, prop), prop.PropertyType, filter);
            body = body == null ? predicate : Expression.AndAlso(body, predicate);
        }

        return body;
    }

    /// <summary>
    /// Builds a single comparison/contains expression for a property access and filter item.
    /// Shared by both direct and two-hop inner predicates.
    /// </summary>
    private static Expression BuildPropertyPredicate(Expression propAccess, Type propType, FilterItem filter)
    {
        var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;
        var op = filter.Operator.ToLowerInvariant();

        if (op == "contains")
        {
            var coalesced = propType == typeof(string)
                ? (Expression)Expression.Coalesce(propAccess, Expression.Constant(string.Empty))
                : propAccess;

            var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
            return Expression.Call(coalesced, containsMethod, Expression.Constant(filter.Value));
        }

        var convertedValue = ConvertFilterValue(filter.Value, underlyingType);
        Expression valueExpr = Expression.Constant(convertedValue, propType.IsValueType && Nullable.GetUnderlyingType(propType) != null
            ? propType : underlyingType);

        if (propType.IsValueType && Nullable.GetUnderlyingType(propType) != null)
            valueExpr = Expression.Constant(convertedValue, propType);

        Expression accessForCompare = propAccess;
        if (Nullable.GetUnderlyingType(propType) != null && op != "equals")
        {
            accessForCompare = Expression.Convert(propAccess, underlyingType);
            valueExpr = Expression.Constant(convertedValue, underlyingType);
        }

        return op switch
        {
            "equals" => Expression.Equal(propAccess, valueExpr),
            "greaterthan" => Expression.GreaterThan(accessForCompare, valueExpr),
            "lessthan" => Expression.LessThan(accessForCompare, valueExpr),
            _ => throw new InvalidOperationException($"Unknown operator '{filter.Operator}'."),
        };
    }

    /// <summary>
    /// Reverse-maps a CLR type to its entity key in <see cref="FieldPropertyMaps"/>.
    /// </summary>
    private static string? GetEntityKeyForType(Type type) => type.Name switch
    {
        nameof(Organisation) => "organisations",
        nameof(Project) => "projects",
        nameof(Event) => "events",
        nameof(Contact) => "contacts",
        nameof(ProjectApplication) => "project_applications",
        _ => null,
    };
}
