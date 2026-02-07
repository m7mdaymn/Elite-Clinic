using EliteClinic.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EliteClinic.Application.Features.Clinic.DTOs;

// ───── Patient DTOs ─────

public class PatientDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }
    public Guid? ParentPatientId { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<PatientSubProfileDto> SubProfiles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class PatientSubProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public bool IsDefault { get; set; }
}

public class CreatePatientRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    public string Phone { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Male;
    public string? Address { get; set; }
    public string? Notes { get; set; }
}

public class CreatePatientResponse
{
    public PatientDto Patient { get; set; } = null!;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdatePatientRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Male;
    public string? Address { get; set; }
    public string? Notes { get; set; }
}

public class AddSubProfileRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Male;
}

public class ResetPasswordResponse
{
    public string NewPassword { get; set; } = string.Empty;
}
