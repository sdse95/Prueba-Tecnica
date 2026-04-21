using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Patients.Api.Features.Auth.Contracts;

namespace Patients.Api.Features.Auth.Application;

public class AuthService(
    UserManager<IdentityUser> userManager,
    IConfiguration configuration) : IAuthService
{
    private const string ReaderRole = "Reader";
    private const string EditorRole = "Editor";

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsRoleSupported(request.Role))
        {
            return (false, $"Invalid role '{request.Role}'. Allowed roles: {ReaderRole}, {EditorRole}.");
        }

        var normalizedUserName = request.UserName.Trim();
        var userExists = await userManager.FindByNameAsync(normalizedUserName);
        if (userExists is not null)
        {
            return (false, "Username already exists.");
        }

        var user = new IdentityUser
        {
            UserName = normalizedUserName
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errorMessage = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return (false, errorMessage);
        }

        var role = string.IsNullOrWhiteSpace(request.Role) ? ReaderRole : request.Role;
        var addRoleResult = await userManager.AddToRoleAsync(user, role);
        if (!addRoleResult.Succeeded)
        {
            var errorMessage = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
            return (false, errorMessage);
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = request.UserName.Trim();
        var user = await userManager.FindByNameAsync(normalizedUserName);
        if (user is null)
        {
            return (false, "Invalid username or password.", null);
        }

        var validPassword = await userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            return (false, "Invalid username or password.", null);
        }

        var roles = await userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? ReaderRole;

        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer not configured.");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience not configured.");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
        var expiresMinutes = int.TryParse(jwtSection["ExpiresMinutes"], out var parsed) ? parsed : 60;

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(expiresMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Role, primaryRole),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var response = new AuthResponse
        {
            Token = tokenString,
            UserName = user.UserName ?? string.Empty,
            Role = primaryRole,
            ExpiresAtUtc = expiresAt
        };

        return (true, null, response);
    }

    private static bool IsRoleSupported(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return true;
        }

        return role is ReaderRole or EditorRole;
    }
}
