namespace CobbleAPI.Models;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: EF Core uses this to represent
    // the one-to-many relationship. A tenant has many users.
    public ICollection<User> Users { get; set; } = new List<User>();

    // Business rule helpers
    // Check if tenant has at least one admin user - I am assuming this is a requirement for a tenant to be valid.
    public bool HasAdmin() => Users.Any(u => u.Role == "Admin");
    public int AdminCount() => Users.Count(u => u.Role == "Admin");
}
