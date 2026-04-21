namespace Patients.Api.Features.Auth.Contracts;

public class AuthResponse
{
    public required string Token { get; set; }
    public required string UserName { get; set; }
    public required string Role { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
