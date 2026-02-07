using EliteClinic.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EliteClinic.Application.Features.Platform.Tenants.DTOs;

public class CreateTenantRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug is required")]
    [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug must be lowercase alphanumeric with hyphens only")]
    [StringLength(100, ErrorMessage = "Slug cannot exceed 100 characters")]
    public string Slug { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? ContactPhone { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; set; }

    public string? LogoUrl { get; set; }
}

public class UpdateTenantRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? ContactPhone { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; set; }

    public string? LogoUrl { get; set; }
}

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public string? ContactPhone { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TenantDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
