namespace CobbleAPI.Models;

public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Tenant Tenant { get; set; }

}
