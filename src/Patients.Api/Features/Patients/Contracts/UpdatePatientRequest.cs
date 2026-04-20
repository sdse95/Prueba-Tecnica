using System.ComponentModel.DataAnnotations;

namespace Patients.Api.Features.Patients.Contracts;

public class UpdatePatientRequest
{
    [MaxLength(10)]
    public string? DocumentType { get; set; }

    [MaxLength(20)]
    public string? DocumentNumber { get; set; }

    [MaxLength(80)]
    public string? FirstName { get; set; }

    [MaxLength(80)]
    public string? LastName { get; set; }

    public DateTime? BirthDate { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(120)]
    [EmailAddress]
    public string? Email { get; set; }
}
