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
builder.Services.AddControllers();
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