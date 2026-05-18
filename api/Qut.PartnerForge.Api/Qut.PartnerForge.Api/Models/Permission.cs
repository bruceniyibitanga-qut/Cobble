namespace Qut.PartnerForge.Api.Models;

/// <summary>Declarative privilege identifier (<c>resource:action</c>) joinable through <see cref="RolePermission"/>.</summary>
public class Permission
{
    public int Id { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
