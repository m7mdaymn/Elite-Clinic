using EliteClinic.Domain.Entities;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EliteClinic.Infrastructure.Data;

public class EliteClinicDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly ITenantContext? _tenantContext;

    // Property for EF Core query filter parameterization (re-evaluated per query)
    private Guid CurrentTenantId => _tenantContext?.IsTenantResolved == true ? _tenantContext.TenantId : Guid.Empty;

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<TenantFeatureFlag> TenantFeatureFlags { get; set; }
    public DbSet<ClinicSettings> ClinicSettings { get; set; }
    public DbSet<WorkingHour> WorkingHours { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<DoctorService> DoctorServices { get; set; }
    public DbSet<DoctorVisitFieldConfig> DoctorVisitFieldConfigs { get; set; }
    public DbSet<Patient> Patients { get; set; }

    public EliteClinicDbContext(DbContextOptions<EliteClinicDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tenant entity configuration
        builder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(100);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Status).HasConversion<int>();
        });

        // AuditLog entity configuration
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired();
            entity.Property(e => e.EntityId).IsRequired();
            entity.Property(e => e.Action).IsRequired();
        });

        // Subscription entity configuration
        builder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlanName).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.Status).HasConversion<int>();
            
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Subscriptions)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // TenantFeatureFlag entity configuration
        builder.Entity<TenantFeatureFlag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId).IsUnique();
            
            entity.HasOne(e => e.Tenant)
                .WithOne(t => t.FeatureFlags)
                .HasForeignKey<TenantFeatureFlag>(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApplicationUser → Tenant FK
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ClinicSettings entity configuration (1:1 with Tenant)
        builder.Entity<ClinicSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.Property(e => e.ClinicName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.WhatsAppSenderNumber).HasMaxLength(20);
            entity.Property(e => e.SupportWhatsAppNumber).HasMaxLength(20);
            entity.Property(e => e.SupportPhoneNumber).HasMaxLength(20);
            entity.Property(e => e.City).HasMaxLength(100);

            entity.HasOne(e => e.Tenant)
                .WithOne()
                .HasForeignKey<ClinicSettings>(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.WorkingHours)
                .WithOne(w => w.ClinicSettings)
                .HasForeignKey(w => w.ClinicSettingsId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // WorkingHour entity configuration
        builder.Entity<WorkingHour>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DayOfWeek).HasConversion<int>();
        });

        // Employee entity configuration (1:1 with ApplicationUser)
        builder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Salary).HasPrecision(18, 2);

            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Doctor entity configuration (1:1 with ApplicationUser)
        builder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Specialty).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.UrgentCaseMode).HasConversion<int>();

            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<Doctor>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Services)
                .WithOne(s => s.Doctor)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.VisitFieldConfig)
                .WithOne(v => v.Doctor)
                .HasForeignKey<DoctorVisitFieldConfig>(v => v.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DoctorService entity configuration
        builder.Entity<DoctorService>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });

        // DoctorVisitFieldConfig entity configuration
        builder.Entity<DoctorVisitFieldConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DoctorId).IsUnique();
        });

        // Patient entity configuration (many patients can share same ApplicationUser for sub-profiles)
        builder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId); // Not unique: parent + sub-profiles share same user
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Gender).HasConversion<int>();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ParentPatient)
                .WithMany(p => p.SubProfiles)
                .HasForeignKey(e => e.ParentPatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Global query filter for tenant-scoped entities
        // Uses property reference so EF Core parameterizes per-query (not captured once at model build)
        builder.Entity<ClinicSettings>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<WorkingHour>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<Doctor>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<DoctorService>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<DoctorVisitFieldConfig>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Handle audit trail before saving
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && e.State != EntityState.Unchanged)
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.Entity is BaseEntity baseEntity)
            {
                baseEntity.UpdatedAt = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    baseEntity.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    baseEntity.IsDeleted = true;
                    baseEntity.DeletedAt = DateTime.UtcNow;
                }
            }
        }

        // Log audit entries
        foreach (var entry in entries)
        {
            var auditLog = CreateAuditLog(entry);
            if (auditLog != null)
            {
                AuditLogs.Add(auditLog);
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private AuditLog? CreateAuditLog(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.Entity is BaseEntity baseEntity && entry.Entity is not AuditLog)
        {
            var action = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => null
            };

            if (action == null)
                return null;

            var userId = _tenantContext?.TenantId;
            var tenantId = entry.Entity is TenantBaseEntity tenantEntity ? tenantEntity.TenantId : _tenantContext?.TenantId;

            var auditLog = new AuditLog(
                userId: userId,
                tenantId: tenantId,
                entityType: entry.Entity.GetType().Name,
                entityId: baseEntity.Id.ToString(),
                action: action
            );

            var oldValues = new Dictionary<string, object>();
            var newValues = new Dictionary<string, object>();

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                    continue;

                if (entry.State == EntityState.Modified)
                {
                    if (property.OriginalValue != null)
                        oldValues[property.Metadata.Name] = property.OriginalValue;

                    if (property.CurrentValue != null)
                        newValues[property.Metadata.Name] = property.CurrentValue;
                }
                else if (entry.State == EntityState.Added && property.CurrentValue != null)
                {
                    newValues[property.Metadata.Name] = property.CurrentValue;
                }
            }

            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            if (oldValues.Any())
                auditLog.OldValues = JsonSerializer.Serialize(oldValues, options);
            if (newValues.Any())
                auditLog.NewValues = JsonSerializer.Serialize(newValues, options);

            return auditLog;
        }

        return null;
    }
}
