using Microsoft.AspNetCore.Identity;

namespace EliteClinic.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; }
    public Guid? TenantId { get; set; }
    public bool IsActive { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public Tenant? Tenant { get; set; }

    public ApplicationUser()
    {
        Id = Guid.NewGuid();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public ApplicationUser(string username, string displayName) : this()
    {
        UserName = username;
        DisplayName = displayName;
        Email = $"{username}@eliteclinic.local";
        NormalizedEmail = Email.ToUpper();
        NormalizedUserName = username.ToUpper();
    }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public ApplicationRole()
    {
        Id = Guid.NewGuid();
    }

    public ApplicationRole(string name) : this()
    {
        Name = name;
        NormalizedName = name.ToUpper();
    }

    public ApplicationRole(string name, string? description) : this(name)
    {
        Description = description;
    }
}
