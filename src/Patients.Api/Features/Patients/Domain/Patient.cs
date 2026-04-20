using System.ComponentModel.DataAnnotations;

namespace Patients.Api.Features.Patients.Domain;

public class Patient
{
    public int PatientId { get; set; }

    [MaxLength(10)]
    public string DocumentType { get; set; } = string.Empty;

    [MaxLength(20)]
    public string DocumentNumber { get; set; } = string.Empty;

    [MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    public DateTime BirthDate { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(120)]
    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; }
}
