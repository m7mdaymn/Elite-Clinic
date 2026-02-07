using EliteClinic.Domain.Entities;
using EliteClinic.Domain.Enums;
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
    public DbSet<QueueSession> QueueSessions { get; set; }
    public DbSet<QueueTicket> QueueTickets { get; set; }
    public DbSet<Visit> Visits { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<LabRequest> LabRequests { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Expense> Expenses { get; set; }

    // Phase 4 entities
    public DbSet<MessageLog> MessageLogs { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<DoctorNote> DoctorNotes { get; set; }
    public DbSet<NotificationSubscription> NotificationSubscriptions { get; set; }

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

        // QueueSession entity configuration
        builder.Entity<QueueSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Tickets)
                .WithOne(t => t.Session)
                .HasForeignKey(t => t.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QueueTicket entity configuration
        builder.Entity<QueueTicket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DoctorService)
                .WithMany()
                .HasForeignKey(e => e.DoctorServiceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Visit entity configuration
        builder.Entity<Visit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Complaint).HasMaxLength(2000);
            entity.Property(e => e.Diagnosis).HasMaxLength(2000);
            entity.Property(e => e.Notes).HasMaxLength(4000);
            entity.Property(e => e.Temperature).HasPrecision(5, 2);
            entity.Property(e => e.Weight).HasPrecision(6, 2);
            entity.Property(e => e.Height).HasPrecision(5, 2);
            entity.Property(e => e.BMI).HasPrecision(5, 2);
            entity.Property(e => e.BloodSugar).HasPrecision(6, 2);
            entity.Property(e => e.OxygenSaturation).HasPrecision(5, 2);

            entity.HasIndex(e => e.QueueTicketId).IsUnique().HasFilter("[QueueTicketId] IS NOT NULL");

            entity.HasOne(e => e.QueueTicket)
                .WithOne()
                .HasForeignKey<Visit>(e => e.QueueTicketId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Prescriptions)
                .WithOne(p => p.Visit)
                .HasForeignKey(p => p.VisitId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.LabRequests)
                .WithOne(l => l.Visit)
                .HasForeignKey(l => l.VisitId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Invoice)
                .WithOne(i => i.Visit)
                .HasForeignKey<Invoice>(i => i.VisitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Prescription entity configuration
        builder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MedicationName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Dosage).HasMaxLength(100);
            entity.Property(e => e.Frequency).HasMaxLength(100);
            entity.Property(e => e.Duration).HasMaxLength(100);
            entity.Property(e => e.Instructions).HasMaxLength(500);
        });

        // LabRequest entity configuration
        builder.Entity<LabRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TestName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ResultText).HasMaxLength(4000);
        });

        // Invoice entity configuration (1:1 with Visit)
        builder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VisitId).IsUnique();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.Property(e => e.RemainingAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Payments)
                .WithOne(p => p.Invoice)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Payment entity configuration (many per Invoice)
        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        // Expense entity configuration
        builder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.RecordedBy)
                .WithMany()
                .HasForeignKey(e => e.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // === Phase 4 Entity Configurations ===

        // MessageLog entity configuration
        builder.Entity<MessageLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RecipientPhone).HasMaxLength(20);
            entity.Property(e => e.Channel).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.FailureReason).HasMaxLength(1000);
            entity.Property(e => e.Variables).HasMaxLength(4000);
        });

        // Booking entity configuration
        builder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DoctorService)
                .WithMany()
                .HasForeignKey(e => e.DoctorServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.QueueTicket)
                .WithMany()
                .HasForeignKey(e => e.QueueTicketId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // DoctorNote entity configuration
        builder.Entity<DoctorNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);

            entity.HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // NotificationSubscription entity configuration
        builder.Entity<NotificationSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(500);
            entity.Property(e => e.P256dh).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Auth).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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
        builder.Entity<QueueSession>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<QueueTicket>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<Visit>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<Prescription>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<LabRequest>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<Invoice>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<Payment>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<Expense>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);

        // Phase 4 query filters
        builder.Entity<MessageLog>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<Booking>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<DoctorNote>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        builder.Entity<NotificationSubscription>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
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

            var userIdStr = _tenantContext?.UserId;
            Guid? userId = !string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var uid) ? uid : _tenantContext?.TenantId;
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
