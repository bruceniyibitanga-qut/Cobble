using Qut.PartnerForge.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Qut.PartnerForge.Api.Data;

/// <summary>
/// Entity Framework Core context for the QUT CRM MySQL schema (snake_case columns, soft deletes on many entities).
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationDbContext"/>.
    /// </summary>
    /// <param name="options">Provider and connection options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    /// <summary>Application roles (<c>admin</c>, <c>course_organiser</c>, <c>industry_partner</c>).</summary>
    public DbSet<Role> Roles { get; set; }

    /// <summary>Named permissions for future fine-grained authorization.</summary>
    public DbSet<Permission> Permissions { get; set; }

    /// <summary>Many-to-many join between roles and permissions.</summary>
    public DbSet<RolePermission> RolePermissions { get; set; }

    /// <summary>Faculties hosting projects and events.</summary>
    public DbSet<Faculty> Faculties { get; set; }

    /// <summary>Industry classification for organisations.</summary>
    public DbSet<Industry> Industries { get; set; }

    /// <summary>Login accounts with optional faculty and organisation anchors.</summary>
    public DbSet<User> Users { get; set; }

    /// <summary>Partner organisations, addresses, and workflow status fields.</summary>
    public DbSet<Organisation> Organisations { get; set; }

    /// <summary>People associated with an organisation.</summary>
    public DbSet<Contact> Contacts { get; set; }

    /// <summary>Published capstone projects.</summary>
    public DbSet<Project> Projects { get; set; }

    /// <summary>Partner-submitted proposals prior to project creation.</summary>
    public DbSet<ProjectApplication> ProjectApplications { get; set; }

    /// <summary>Calendar events and info sessions.</summary>
    public DbSet<Event> Events { get; set; }

    /// <summary>RSVP-style links between events and organisations.</summary>
    public DbSet<EventAttendance> EventAttendances { get; set; }

    /// <summary>Append-only change history with JSON snapshots.</summary>
    public DbSet<AuditLog> AuditLog { get; set; }

    /// <inheritdoc />
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

        // AuditLog — JSON columns for old/new row snapshots
        modelBuilder.Entity<AuditLog>()
            .Property(a => a.OldValues).HasColumnType("json");
        modelBuilder.Entity<AuditLog>()
            .Property(a => a.NewValues).HasColumnType("json");

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

    /// <summary>
    /// Converts PascalCase identifiers to snake_case for table and column mapping.
    /// </summary>
    private static string ToSnakeCase(string name) =>
        Regex.Replace(
            Regex.Replace(name, @"([A-Z]+)([A-Z][a-z])", "$1_$2"),
            @"([a-z\d])([A-Z])", "$1_$2"
        ).ToLower();
}
