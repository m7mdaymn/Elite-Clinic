using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Auth.Services;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Domain.Entities;
using EliteClinic.Infrastructure.Data;
using EliteClinic.Infrastructure.Middleware;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Elite Clinic API",
        Version = "0.0.1",
        Description = "Multi-tenant clinic SaaS platform",
    });

    options.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Bearer token"
    });

    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    "Server=db40278.public.databaseasp.net;Database=db40278;User Id=db40278;Password=5Fq@k+D3-N9c;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
builder.Services.AddDbContext<EliteClinicDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
})
.AddEntityFrameworkStores<EliteClinicDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "your-secret-key-that-is-very-long-and-secure-at-least-256-bits";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "EliteClinic",
        ValidAudience = jwtSettings["Audience"] ?? "EliteClinicUsers",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Services
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Phase 1 Platform Services
builder.Services.AddScoped<EliteClinic.Application.Features.Platform.Tenants.Services.ITenantService, 
    EliteClinic.Application.Features.Platform.Tenants.Services.TenantService>();
builder.Services.AddScoped<EliteClinic.Application.Features.Platform.Subscriptions.Services.ISubscriptionService, 
    EliteClinic.Application.Features.Platform.Subscriptions.Services.SubscriptionService>();
builder.Services.AddScoped<EliteClinic.Application.Features.Platform.FeatureFlags.Services.IFeatureFlagService, 
    EliteClinic.Application.Features.Platform.FeatureFlags.Services.FeatureFlagService>();

// Phase 2 Clinic Services
builder.Services.AddScoped<IClinicSettingsService, ClinicSettingsService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IDoctorService, DoctorServiceImpl>();
builder.Services.AddScoped<IPatientService, PatientService>();

// Phase 3 Services
builder.Services.AddScoped<IQueueService, QueueService>();
builder.Services.AddScoped<IVisitService, VisitService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<ILabRequestService, LabRequestService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();

// RF06 Fix: Wrap model validation errors in ApiResponse format
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(err => (object)new
            {
                field = e.Key,
                message = err.ErrorMessage
            }))
            .ToList();

        var response = ApiResponse<object>.ValidationError(errors, "Validation failed");
        return new BadRequestObjectResult(response);
    };
});

// Cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EliteClinicDbContext>();
    dbContext.Database.Migrate();
    
    // Seed initial data
    await SeedDataAsync(dbContext, scope.ServiceProvider);
}

// Configure HTTP pipeline
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Elite Clinic API v1");
});

app.UseHttpsRedirection();
app.UseCors();
app.UseMiddleware<EliteClinic.Infrastructure.Middleware.TenantMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

async Task SeedDataAsync(EliteClinicDbContext dbContext, IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    // Seed roles
    var roles = new[] { "SuperAdmin", "ClinicOwner", "ClinicManager", "Doctor", "Patient" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new ApplicationRole(role));
        }
    }

    // Seed SuperAdmin user
    var adminUsername = "superadmin";
    if (await userManager.FindByNameAsync(adminUsername) == null)
    {
        var admin = new ApplicationUser(adminUsername, "Platform Admin")
        {
            Email = "admin@eliteclinic.local",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, "Admin@123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "SuperAdmin");
        }
    }

    // Phase 1: Seed Tenants for testing (idempotent)
    await SeedPhase1TenantsAsync(dbContext);

    // Phase 1.5: Seed ClinicSettings for existing tenants that are missing them
    await SeedClinicSettingsAsync(dbContext);

    // Phase 2: Seed clinic users for testing (idempotent)
    await SeedPhase2ClinicUsersAsync(dbContext, userManager);

    // Phase 3: Seed queue sessions, visits, invoices, expenses (idempotent)
    await SeedPhase3WorkflowAsync(dbContext);
}

async Task SeedPhase1TenantsAsync(EliteClinicDbContext dbContext)
{
    // Check if tenants already exist
    if (await dbContext.Tenants.AnyAsync(t => t.Slug == "demo-clinic"))
    {
        return; // Already seeded
    }

    // Tenant 1: Active (for happy path testing)
    var activeTenant = new EliteClinic.Domain.Entities.Tenant("Demo Clinic", "demo-clinic")
    {
        ContactPhone = "+201234567890",
        Address = "123 Main St, Cairo, Egypt",
        LogoUrl = "https://example.com/demo-clinic-logo.png",
        Status = EliteClinic.Domain.Enums.TenantStatus.Active
    };
    dbContext.Tenants.Add(activeTenant);

    // Tenant 2: Suspended (for middleware testing)
    var suspendedTenant = new EliteClinic.Domain.Entities.Tenant("Suspended Clinic", "suspended-clinic")
    {
        ContactPhone = "+201234567891",
        Status = EliteClinic.Domain.Enums.TenantStatus.Suspended
    };
    dbContext.Tenants.Add(suspendedTenant);

    // Tenant 3: Blocked (for middleware testing)
    var blockedTenant = new EliteClinic.Domain.Entities.Tenant("Blocked Clinic", "blocked-clinic")
    {
        ContactPhone = "+201234567892",
        Status = EliteClinic.Domain.Enums.TenantStatus.Blocked
    };
    dbContext.Tenants.Add(blockedTenant);

    // Tenant 4: Inactive (for middleware testing)
    var inactiveTenant = new EliteClinic.Domain.Entities.Tenant("Inactive Clinic", "inactive-clinic")
    {
        ContactPhone = "+201234567893",
        Status = EliteClinic.Domain.Enums.TenantStatus.Inactive
    };
    dbContext.Tenants.Add(inactiveTenant);

    await dbContext.SaveChangesAsync();

    // Auto-create feature flags for all tenants
    foreach (var tenant in new[] { activeTenant, suspendedTenant, blockedTenant, inactiveTenant })
    {
        var featureFlags = new EliteClinic.Domain.Entities.TenantFeatureFlag
        {
            TenantId = tenant.Id,
            OnlineBooking = false,
            WhatsappAutomation = true,
            PwaNotifications = false,
            ExpensesModule = true,
            AdvancedMedicalTemplates = false,
            Ratings = false,
            Export = false
        };
        dbContext.TenantFeatureFlags.Add(featureFlags);
    }

    await dbContext.SaveChangesAsync();

    // Auto-create ClinicSettings for all tenants
    foreach (var tenant in new[] { activeTenant, suspendedTenant, blockedTenant, inactiveTenant })
    {
        var clinicSettings = new EliteClinic.Domain.Entities.ClinicSettings(tenant.Id, tenant.Name)
        {
            Phone = tenant.ContactPhone,
            Address = tenant.Address,
            LogoUrl = tenant.LogoUrl
        };
        dbContext.ClinicSettings.Add(clinicSettings);
    }

    await dbContext.SaveChangesAsync();

    // Seed subscriptions for the Active tenant only
    var now = DateTime.UtcNow;

    // Subscription 1: Active, unpaid
    var unpaidSubscription = new EliteClinic.Domain.Entities.Subscription
    {
        TenantId = activeTenant.Id,
        PlanName = "Basic Monthly",
        StartDate = now.AddDays(-5),
        EndDate = now.AddDays(25),
        Amount = 500.00m,
        Currency = "EGP",
        IsPaid = false,
        Status = EliteClinic.Domain.Enums.SubscriptionStatus.Active,
        Notes = "Monthly subscription - payment pending"
    };
    dbContext.Subscriptions.Add(unpaidSubscription);

    // Subscription 2: Active, paid
    var paidSubscription = new EliteClinic.Domain.Entities.Subscription
    {
        TenantId = activeTenant.Id,
        PlanName = "Premium Quarterly",
        StartDate = now.AddDays(-10),
        EndDate = now.AddDays(80),
        Amount = 1200.00m,
        Currency = "EGP",
        IsPaid = true,
        PaidAt = now.AddDays(-10),
        PaymentMethod = "Bank Transfer",
        PaymentReference = "TXN-2026-001",
        Status = EliteClinic.Domain.Enums.SubscriptionStatus.Active,
        Notes = "Quarterly plan - fully paid"
    };
    dbContext.Subscriptions.Add(paidSubscription);

    // Subscription 3: Expired (for extend testing)
    var expiredSubscription = new EliteClinic.Domain.Entities.Subscription
    {
        TenantId = activeTenant.Id,
        PlanName = "Trial Plan",
        StartDate = now.AddDays(-40),
        EndDate = now.AddDays(-10),
        Amount = 0.00m,
        Currency = "EGP",
        IsPaid = true,
        PaidAt = now.AddDays(-40),
        PaymentMethod = "Free Trial",
        Status = EliteClinic.Domain.Enums.SubscriptionStatus.Expired,
        Notes = "Trial period ended"
    };
    dbContext.Subscriptions.Add(expiredSubscription);

    // Subscription 4: Cancelled (to test extend rejection)
    var cancelledSubscription = new EliteClinic.Domain.Entities.Subscription
    {
        TenantId = activeTenant.Id,
        PlanName = "Annual Plan",
        StartDate = now.AddDays(-60),
        EndDate = now.AddDays(-30),
        Amount = 5000.00m,
        Currency = "EGP",
        IsPaid = false,
        Status = EliteClinic.Domain.Enums.SubscriptionStatus.Cancelled,
        CancelledAt = now.AddDays(-30),
        CancelReason = "Customer requested cancellation due to budget constraints",
        Notes = "Cancelled before payment"
    };
    dbContext.Subscriptions.Add(cancelledSubscription);

    await dbContext.SaveChangesAsync();
}

async Task SeedClinicSettingsAsync(EliteClinicDbContext dbContext)
{
    // Idempotent: only create ClinicSettings for tenants that don't have one yet
    var tenantsWithoutSettings = await dbContext.Tenants
        .Where(t => !t.IsDeleted)
        .Where(t => !dbContext.ClinicSettings.IgnoreQueryFilters().Any(cs => cs.TenantId == t.Id && !cs.IsDeleted))
        .ToListAsync();

    if (!tenantsWithoutSettings.Any())
        return;

    foreach (var tenant in tenantsWithoutSettings)
    {
        var clinicSettings = new EliteClinic.Domain.Entities.ClinicSettings(tenant.Id, tenant.Name)
        {
            Phone = tenant.ContactPhone,
            Address = tenant.Address,
            LogoUrl = tenant.LogoUrl
        };
        dbContext.ClinicSettings.Add(clinicSettings);
    }

    await dbContext.SaveChangesAsync();
}

async Task SeedPhase2ClinicUsersAsync(EliteClinicDbContext dbContext, UserManager<ApplicationUser> userManager)
{
    var demoTenant = await dbContext.Tenants.IgnoreQueryFilters()
        .FirstOrDefaultAsync(t => t.Slug == "demo-clinic" && !t.IsDeleted);
    if (demoTenant == null) return;

    var tenantId = demoTenant.Id;
    var patientsExist = await dbContext.Patients.IgnoreQueryFilters().AnyAsync();

    // Helper: get or create user
    async Task<ApplicationUser> GetOrCreateUser(string username, string displayName, string password, string role)
    {
        var existing = await userManager.FindByNameAsync(username);
        if (existing != null)
        {
            // Ensure password matches expected value (idempotent seed)
            await userManager.RemovePasswordAsync(existing);
            await userManager.AddPasswordAsync(existing, password);
            return existing;
        }
        var user = new ApplicationUser(username, displayName) { TenantId = tenantId, IsActive = true };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
        return user;
    }

    // Always reset passwords for existing users
    // 1. ClinicOwner
    await GetOrCreateUser("owner_demo", "Dr. Ahmed Clinic Owner", "Owner@123456", "ClinicOwner");

    // 2. Staff member 1 (ClinicManager)
    var staff1 = await GetOrCreateUser("staff_sara", "Sara Reception", "Staff@123456", "ClinicManager");
    if (!await dbContext.Employees.IgnoreQueryFilters().AnyAsync(e => e.UserId == staff1.Id))
    {
        dbContext.Employees.Add(new EliteClinic.Domain.Entities.Employee
        {
            TenantId = tenantId, UserId = staff1.Id, Name = "Sara Reception",
            Phone = "+201111111111", Role = "ClinicManager", Salary = 5000m,
            HireDate = DateTime.UtcNow.AddMonths(-6), IsEnabled = true
        });
    }

    // 3. Staff member 2 (ClinicManager)
    var staff2 = await GetOrCreateUser("staff_ali", "Ali Front Desk", "Staff@123456", "ClinicManager");
    if (!await dbContext.Employees.IgnoreQueryFilters().AnyAsync(e => e.UserId == staff2.Id))
    {
        dbContext.Employees.Add(new EliteClinic.Domain.Entities.Employee
        {
            TenantId = tenantId, UserId = staff2.Id, Name = "Ali Front Desk",
            Phone = "+201111111112", Role = "ClinicManager", Salary = 4500m,
            HireDate = DateTime.UtcNow.AddMonths(-3), IsEnabled = true
        });
    }

    // 4. Doctor 1
    var doc1User = await GetOrCreateUser("dr_khaled", "Dr. Khaled Dentist", "Doctor@123456", "Doctor");
    if (!await dbContext.Doctors.IgnoreQueryFilters().AnyAsync(d => d.UserId == doc1User.Id))
    {
        var doctor1 = new EliteClinic.Domain.Entities.Doctor
        {
            TenantId = tenantId, UserId = doc1User.Id, Name = "Dr. Khaled Dentist",
            Specialty = "General Dentistry", Phone = "+201222222221",
            Bio = "10 years experience in general and cosmetic dentistry",
            IsEnabled = true, UrgentCaseMode = EliteClinic.Domain.Enums.UrgentCaseMode.UrgentNext,
            AvgVisitDurationMinutes = 20
        };
        dbContext.Doctors.Add(doctor1);

        dbContext.DoctorServices.AddRange(
            new EliteClinic.Domain.Entities.DoctorService { TenantId = tenantId, DoctorId = doctor1.Id, ServiceName = "Consultation", Price = 200m, DurationMinutes = 15, IsActive = true },
            new EliteClinic.Domain.Entities.DoctorService { TenantId = tenantId, DoctorId = doctor1.Id, ServiceName = "Root Canal", Price = 1500m, DurationMinutes = 60, IsActive = true },
            new EliteClinic.Domain.Entities.DoctorService { TenantId = tenantId, DoctorId = doctor1.Id, ServiceName = "Teeth Whitening", Price = 2000m, DurationMinutes = 45, IsActive = true }
        );

        dbContext.DoctorVisitFieldConfigs.Add(new EliteClinic.Domain.Entities.DoctorVisitFieldConfig
        {
            TenantId = tenantId, DoctorId = doctor1.Id, Temperature = true, Weight = true, BloodPressure = true
        });
    }

    // 5. Doctor 2
    var doc2User = await GetOrCreateUser("dr_mona", "Dr. Mona Orthodontist", "Doctor@123456", "Doctor");
    if (!await dbContext.Doctors.IgnoreQueryFilters().AnyAsync(d => d.UserId == doc2User.Id))
    {
        var doctor2 = new EliteClinic.Domain.Entities.Doctor
        {
            TenantId = tenantId, UserId = doc2User.Id, Name = "Dr. Mona Orthodontist",
            Specialty = "Orthodontics", Phone = "+201222222222",
            Bio = "Specialist in braces and aligners", IsEnabled = true,
            UrgentCaseMode = EliteClinic.Domain.Enums.UrgentCaseMode.UrgentBucket,
            AvgVisitDurationMinutes = 30
        };
        dbContext.Doctors.Add(doctor2);

        dbContext.DoctorServices.AddRange(
            new EliteClinic.Domain.Entities.DoctorService { TenantId = tenantId, DoctorId = doctor2.Id, ServiceName = "Braces Consultation", Price = 300m, DurationMinutes = 20, IsActive = true },
            new EliteClinic.Domain.Entities.DoctorService { TenantId = tenantId, DoctorId = doctor2.Id, ServiceName = "Braces Adjustment", Price = 500m, DurationMinutes = 30, IsActive = true }
        );

        dbContext.DoctorVisitFieldConfigs.Add(new EliteClinic.Domain.Entities.DoctorVisitFieldConfig
        {
            TenantId = tenantId, DoctorId = doctor2.Id, Temperature = true, Weight = true
        });
    }

    await dbContext.SaveChangesAsync();

    // 6. Patients (6 + 1 sub-profile)
    var patientNames = new[]
    {
        ("Mohamed Hassan", "+201500000001", EliteClinic.Domain.Enums.Gender.Male),
        ("Fatma Ali", "+201500000002", EliteClinic.Domain.Enums.Gender.Female),
        ("Youssef Mahmoud", "+201500000003", EliteClinic.Domain.Enums.Gender.Male),
        ("Nour Ibrahim", "+201500000004", EliteClinic.Domain.Enums.Gender.Female),
        ("Omar Tarek", "+201500000005", EliteClinic.Domain.Enums.Gender.Male),
        ("Salma Ahmed", "+201500000006", EliteClinic.Domain.Enums.Gender.Female)
    };

    EliteClinic.Domain.Entities.Patient? firstPatient = null;
    ApplicationUser? firstPatientUser = null;

    for (int i = 0; i < patientNames.Length; i++)
    {
        var (name, phone, gender) = patientNames[i];
        var username = $"patient_demo-clinic_{i + 1}";
        var patientUser = await GetOrCreateUser(username, name, "Patient@1234", "Patient");

        // Idempotent: skip if patient already exists for this user
        if (await dbContext.Patients.IgnoreQueryFilters().AnyAsync(p => p.UserId == patientUser.Id && p.IsDefault && !p.IsDeleted))
        {
            if (i == 0) {
                firstPatient = await dbContext.Patients.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.UserId == patientUser.Id && p.IsDefault && !p.IsDeleted);
                firstPatientUser = patientUser;
            }
            continue;
        }

        var patient = new EliteClinic.Domain.Entities.Patient
        {
            TenantId = tenantId, UserId = patientUser.Id, Name = name,
            Phone = phone, Gender = gender,
            DateOfBirth = DateTime.UtcNow.AddYears(-(20 + i * 5)),
            IsDefault = true, ParentPatientId = null
        };
        dbContext.Patients.Add(patient);

        if (i == 0) { firstPatient = patient; firstPatientUser = patientUser; }
    }

    await dbContext.SaveChangesAsync();

    // Add sub-profile for first patient
    if (firstPatient != null && firstPatientUser != null)
    {
        if (!await dbContext.Patients.IgnoreQueryFilters().AnyAsync(p => p.UserId == firstPatientUser.Id && !p.IsDefault && !p.IsDeleted))
        {
            dbContext.Patients.Add(new EliteClinic.Domain.Entities.Patient
        {
            TenantId = tenantId, UserId = firstPatientUser.Id,
            Name = "Yassin Mohamed", Phone = firstPatient.Phone,
            Gender = EliteClinic.Domain.Enums.Gender.Male,
            DateOfBirth = DateTime.UtcNow.AddYears(-5),
            IsDefault = false, ParentPatientId = firstPatient.Id
        });
        await dbContext.SaveChangesAsync();
        }
    }
}

async Task SeedPhase3WorkflowAsync(EliteClinicDbContext dbContext)
{
    // Idempotent: skip if any queue sessions exist
    if (await dbContext.QueueSessions.IgnoreQueryFilters().AnyAsync())
        return;

    var demoTenant = await dbContext.Tenants.IgnoreQueryFilters()
        .FirstOrDefaultAsync(t => t.Slug == "demo-clinic" && !t.IsDeleted);
    if (demoTenant == null) return;

    var tenantId = demoTenant.Id;

    // Get seeded doctors and patients
    var doctors = await dbContext.Doctors.IgnoreQueryFilters()
        .Where(d => d.TenantId == tenantId && !d.IsDeleted).ToListAsync();
    var patients = await dbContext.Patients.IgnoreQueryFilters()
        .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.IsDefault).ToListAsync();
    var doctorServices = await dbContext.DoctorServices.IgnoreQueryFilters()
        .Where(ds => ds.TenantId == tenantId && !ds.IsDeleted).ToListAsync();
    var staff = await dbContext.Employees.IgnoreQueryFilters()
        .Where(e => e.TenantId == tenantId && !e.IsDeleted).FirstOrDefaultAsync();

    if (doctors.Count < 2 || patients.Count < 4) return;

    var doctor1 = doctors[0]; // Dr. Khaled
    var doctor2 = doctors[1]; // Dr. Mona
    var now = DateTime.UtcNow;

    // === Queue Session 1: Dr. Khaled (today, active) ===
    var session1 = new EliteClinic.Domain.Entities.QueueSession
    {
        TenantId = tenantId, DoctorId = doctor1.Id, IsActive = true, StartedAt = now.AddHours(-3)
    };
    dbContext.QueueSessions.Add(session1);

    // Ticket 1: Completed visit (patient 1)
    var ticket1 = new EliteClinic.Domain.Entities.QueueTicket
    {
        TenantId = tenantId, SessionId = session1.Id, PatientId = patients[0].Id,
        DoctorId = doctor1.Id, DoctorServiceId = doctorServices.First(ds => ds.DoctorId == doctor1.Id).Id,
        TicketNumber = 1, Status = EliteClinic.Domain.Enums.TicketStatus.Completed,
        CalledAt = now.AddHours(-2.5), VisitStartedAt = now.AddHours(-2.5), CompletedAt = now.AddHours(-2)
    };
    dbContext.QueueTickets.Add(ticket1);

    // Visit for ticket 1 (completed)
    var visit1 = new EliteClinic.Domain.Entities.Visit
    {
        TenantId = tenantId, QueueTicketId = ticket1.Id, DoctorId = doctor1.Id,
        PatientId = patients[0].Id, Status = EliteClinic.Domain.Enums.VisitStatus.Completed,
        Complaint = "Toothache in lower right molar",
        Diagnosis = "Dental caries in tooth #46",
        Notes = "Recommended root canal treatment",
        BloodPressureSystolic = 120, BloodPressureDiastolic = 80, HeartRate = 72, Temperature = 36.8m, Weight = 78.5m
    };
    dbContext.Visits.Add(visit1);

    // Prescriptions for visit 1
    dbContext.Prescriptions.Add(new EliteClinic.Domain.Entities.Prescription
    {
        TenantId = tenantId, VisitId = visit1.Id,
        MedicationName = "Amoxicillin 500mg", Dosage = "500mg", Frequency = "3 times daily",
        Duration = "7 days", Instructions = "Take after meals"
    });
    dbContext.Prescriptions.Add(new EliteClinic.Domain.Entities.Prescription
    {
        TenantId = tenantId, VisitId = visit1.Id,
        MedicationName = "Ibuprofen 400mg", Dosage = "400mg", Frequency = "As needed",
        Duration = "5 days", Instructions = "Take with food for pain relief"
    });

    // Lab request for visit 1
    dbContext.LabRequests.Add(new EliteClinic.Domain.Entities.LabRequest
    {
        TenantId = tenantId, VisitId = visit1.Id,
        TestName = "Dental X-Ray Periapical", Type = EliteClinic.Domain.Enums.LabRequestType.Imaging,
        IsUrgent = false, ResultText = "Caries visible on #46, no periapical abscess",
        ResultReceivedAt = now.AddHours(-1)
    });

    // Invoice for visit 1 (fully paid)
    var invoice1 = new EliteClinic.Domain.Entities.Invoice
    {
        TenantId = tenantId, VisitId = visit1.Id, PatientId = patients[0].Id, DoctorId = doctor1.Id,
        Amount = 200m, PaidAmount = 200m, RemainingAmount = 0m,
        Status = EliteClinic.Domain.Enums.InvoiceStatus.Paid
    };
    dbContext.Invoices.Add(invoice1);

    dbContext.Payments.Add(new EliteClinic.Domain.Entities.Payment
    {
        TenantId = tenantId, InvoiceId = invoice1.Id,
        Amount = 200m, PaymentMethod = "Cash", PaidAt = now.AddHours(-2)
    });

    // Ticket 2: Currently in visit (patient 2)
    var ticket2 = new EliteClinic.Domain.Entities.QueueTicket
    {
        TenantId = tenantId, SessionId = session1.Id, PatientId = patients[1].Id,
        DoctorId = doctor1.Id, DoctorServiceId = doctorServices.First(ds => ds.DoctorId == doctor1.Id).Id,
        TicketNumber = 2, Status = EliteClinic.Domain.Enums.TicketStatus.InVisit,
        CalledAt = now.AddMinutes(-30), VisitStartedAt = now.AddMinutes(-30)
    };
    dbContext.QueueTickets.Add(ticket2);

    var visit2 = new EliteClinic.Domain.Entities.Visit
    {
        TenantId = tenantId, QueueTicketId = ticket2.Id, DoctorId = doctor1.Id,
        PatientId = patients[1].Id, Status = EliteClinic.Domain.Enums.VisitStatus.Open,
        Complaint = "Routine dental checkup", Temperature = 36.5m, Weight = 65m
    };
    dbContext.Visits.Add(visit2);

    // Ticket 3: Waiting (patient 3)
    dbContext.QueueTickets.Add(new EliteClinic.Domain.Entities.QueueTicket
    {
        TenantId = tenantId, SessionId = session1.Id, PatientId = patients[2].Id,
        DoctorId = doctor1.Id, TicketNumber = 3, Status = EliteClinic.Domain.Enums.TicketStatus.Waiting
    });

    // Ticket 4: Waiting + Urgent (patient 4)
    dbContext.QueueTickets.Add(new EliteClinic.Domain.Entities.QueueTicket
    {
        TenantId = tenantId, SessionId = session1.Id, PatientId = patients[3].Id,
        DoctorId = doctor1.Id, TicketNumber = 4, Status = EliteClinic.Domain.Enums.TicketStatus.Waiting,
        IsUrgent = true
    });

    // === Queue Session 2: Dr. Mona (today, active) ===
    var session2 = new EliteClinic.Domain.Entities.QueueSession
    {
        TenantId = tenantId, DoctorId = doctor2.Id, IsActive = true, StartedAt = now.AddHours(-2)
    };
    dbContext.QueueSessions.Add(session2);

    // Ticket for Dr. Mona (waiting)
    dbContext.QueueTickets.Add(new EliteClinic.Domain.Entities.QueueTicket
    {
        TenantId = tenantId, SessionId = session2.Id, PatientId = patients[4].Id,
        DoctorId = doctor2.Id, DoctorServiceId = doctorServices.First(ds => ds.DoctorId == doctor2.Id).Id,
        TicketNumber = 1, Status = EliteClinic.Domain.Enums.TicketStatus.Waiting
    });

    // === Manual Visit (no ticket, yesterday) ===
    var manualVisit = new EliteClinic.Domain.Entities.Visit
    {
        TenantId = tenantId, QueueTicketId = null, DoctorId = doctor2.Id,
        PatientId = patients[5].Id, Status = EliteClinic.Domain.Enums.VisitStatus.Completed,
        Complaint = "Braces adjustment follow-up",
        Diagnosis = "Adjustment completed successfully",
        Notes = "Next appointment in 4 weeks",
        FollowUpDate = now.AddDays(28)
    };
    // Set CreatedAt to yesterday so it appears as a past visit
    dbContext.Visits.Add(manualVisit);

    // Invoice for manual visit (partially paid)
    var invoice3 = new EliteClinic.Domain.Entities.Invoice
    {
        TenantId = tenantId, VisitId = manualVisit.Id, PatientId = patients[5].Id, DoctorId = doctor2.Id,
        Amount = 500m, PaidAmount = 300m, RemainingAmount = 200m,
        Status = EliteClinic.Domain.Enums.InvoiceStatus.PartiallyPaid
    };
    dbContext.Invoices.Add(invoice3);

    dbContext.Payments.Add(new EliteClinic.Domain.Entities.Payment
    {
        TenantId = tenantId, InvoiceId = invoice3.Id,
        Amount = 300m, PaymentMethod = "Credit Card", ReferenceNumber = "CC-2025-001",
        PaidAt = now.AddDays(-1)
    });

    // Lab request for manual visit (pending result)
    dbContext.LabRequests.Add(new EliteClinic.Domain.Entities.LabRequest
    {
        TenantId = tenantId, VisitId = manualVisit.Id,
        TestName = "Panoramic X-Ray", Type = EliteClinic.Domain.Enums.LabRequestType.Imaging,
        IsUrgent = false
    });

    // === Expenses ===
    var recordedByUserId = staff?.UserId ?? doctor1.UserId;

    dbContext.Expenses.Add(new EliteClinic.Domain.Entities.Expense
    {
        TenantId = tenantId, Category = "Supplies",
        Amount = 1500m, Notes = "Dental supplies and consumables",
        ExpenseDate = now.Date, RecordedByUserId = recordedByUserId
    });

    dbContext.Expenses.Add(new EliteClinic.Domain.Entities.Expense
    {
        TenantId = tenantId, Category = "Utilities",
        Amount = 800m, Notes = "Electricity and water bill",
        ExpenseDate = now.Date.AddDays(-5), RecordedByUserId = recordedByUserId
    });

    dbContext.Expenses.Add(new EliteClinic.Domain.Entities.Expense
    {
        TenantId = tenantId, Category = "Rent",
        Amount = 5000m, Notes = "Monthly clinic rent",
        ExpenseDate = now.Date.AddDays(-15), RecordedByUserId = recordedByUserId
    });

    await dbContext.SaveChangesAsync();
}