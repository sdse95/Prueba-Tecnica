using System.ComponentModel.DataAnnotations;

namespace Patients.Api.Features.Patients.Contracts;

public class CreatePatientRequest
{
    [Required]
    [MaxLength(10)]
    public string DocumentType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateTime BirthDate { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(120)]
    [EmailAddress]
    public string? Email { get; set; }
}
