using CobbleAPI.Interfaces;
using CobbleAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CobbleAPI.Data;

public class ApplicationDbContext : DbContext
{
    private readonly ITenantService _tenantService;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService tenantService) : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<User> User => Set<User>();
    public DbSet<Tenant> Tenant => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure the User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            
            entity.HasOne(u => u.Tenant).WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        // Configure the Tenant entity
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(t => t.Name).IsUnique();
        });

        // Seed data for testing
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var adminId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<Tenant>().HasData(new Tenant
        {
            Id = tenantId,
            Name = "System Administration",
            CreatedAt = DateTime.UtcNow
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminId,
            Email = "admin@system.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FullName = "System Administrator",
            Role = "Admin",
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        });
    }
}
