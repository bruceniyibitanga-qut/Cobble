namespace Qut.PartnerForge.Api.Models;

/// <summary>Composite key row wiring <see cref="Role"/> to <see cref="Permission"/>.</summary>
public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
