using CobbleAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CobbleAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Faculty> Faculties { get; set; }
    public DbSet<Industry> Industries { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Organisation> Organisations { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectApplication> ProjectApplications { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventAttendance> EventAttendances { get; set; }
    public DbSet<AuditLog> AuditLog { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite PK for join table
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Role → Users
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId);

        // Faculty → Users (optional)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Faculty)
            .WithMany(f => f.Users)
            .HasForeignKey(u => u.FacultyId)
            .IsRequired(false);

        // Organisation → Users (optional — industry partner users link to their org)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Organisation)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganisationId)
            .IsRequired(false);

        // Industry → Organisations (optional)
        modelBuilder.Entity<Organisation>()
            .HasOne(o => o.Industry)
            .WithMany(i => i.Organisations)
            .HasForeignKey(o => o.IndustryId)
            .IsRequired(false);

        // Organisation → Contacts
        modelBuilder.Entity<Contact>()
            .HasOne(c => c.Organisation)
            .WithMany(o => o.Contacts)
            .HasForeignKey(c => c.OrganisationId);

        // Organisation → Projects
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Organisation)
            .WithMany(o => o.Projects)
            .HasForeignKey(p => p.OrganisationId);

        // Faculty → Projects (optional)
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Faculty)
            .WithMany(f => f.Projects)
            .HasForeignKey(p => p.FacultyId)
            .IsRequired(false);

        // Organisation → ProjectApplications
        modelBuilder.Entity<ProjectApplication>()
            .HasOne(pa => pa.Organisation)
            .WithMany(o => o.ProjectApplications)
            .HasForeignKey(pa => pa.OrganisationId);

        // Contact → ProjectApplications (optional)
        modelBuilder.Entity<ProjectApplication>()
            .HasOne(pa => pa.Contact)
            .WithMany(c => c.ProjectApplications)
            .HasForeignKey(pa => pa.ContactId)
            .IsRequired(false);

        // Faculty → ProjectApplications via ProposedFacultyId (no inverse collection on Faculty)
        modelBuilder.Entity<ProjectApplication>()
            .HasOne(pa => pa.ProposedFaculty)
            .WithMany()
            .HasForeignKey(pa => pa.ProposedFacultyId)
            .IsRequired(false);

        // Project → ProjectApplications via ResultingProjectId
        modelBuilder.Entity<ProjectApplication>()
            .HasOne(pa => pa.ResultingProject)
            .WithMany(p => p.Applications)
            .HasForeignKey(pa => pa.ResultingProjectId)
            .IsRequired(false);

        // Faculty → Events (optional)
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Faculty)
            .WithMany(f => f.Events)
            .HasForeignKey(e => e.FacultyId)
            .IsRequired(false);

        // Event → EventAttendances
        modelBuilder.Entity<EventAttendance>()
            .HasOne(ea => ea.Event)
            .WithMany(e => e.Attendances)
            .HasForeignKey(ea => ea.EventId);

        // Organisation → EventAttendances
        modelBuilder.Entity<EventAttendance>()
            .HasOne(ea => ea.Organisation)
            .WithMany(o => o.EventAttendances)
            .HasForeignKey(ea => ea.OrganisationId);

        // AuditLog — JSONB columns; inet is left as text (Postgres casts on read)
        modelBuilder.Entity<AuditLog>()
            .Property(a => a.OldValues).HasColumnType("jsonb");
        modelBuilder.Entity<AuditLog>()
            .Property(a => a.NewValues).HasColumnType("jsonb");

        // Apply snake_case naming to all tables and columns to match schema.sql
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName != null)
                entityType.SetTableName(ToSnakeCase(tableName));

            foreach (var property in entityType.GetProperties())
                property.SetColumnName(ToSnakeCase(property.Name));
        }
    }

    private static string ToSnakeCase(string name) =>
        Regex.Replace(
            Regex.Replace(name, @"([A-Z]+)([A-Z][a-z])", "$1_$2"),
            @"([a-z\d])([A-Z])", "$1_$2"
        ).ToLower();
}
