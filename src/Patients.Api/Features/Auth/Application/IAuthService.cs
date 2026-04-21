using Patients.Api.Features.Auth.Contracts;

namespace Patients.Api.Features.Auth.Application;

public interface IAuthService
{
    Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
