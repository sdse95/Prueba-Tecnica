using System.ComponentModel.DataAnnotations;

namespace Patients.Api.Features.Auth.Contracts;

public class LoginRequest
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
