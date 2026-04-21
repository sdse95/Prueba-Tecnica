using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Patients.Api.Features.Auth.Application;
using Patients.Api.Features.Auth.Contracts;
using Patients.Api.Infrastructure.Persistence;

namespace Patients.Api.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUser_WithRequestedRole()
    {
        var (authService, userManager, roleManager) = BuildAuthFixture();
        await roleManager.CreateAsync(new IdentityRole("Reader"));

        var (success, error) = await authService.RegisterAsync(
            new RegisterRequest
            {
                UserName = "reader1",
                Password = "Reader123!",
                Role = "Reader"
            },
            CancellationToken.None);

        Assert.True(success);
        Assert.Null(error);

        var user = await userManager.FindByNameAsync("reader1");
        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user!, "Reader"));
    }

    [Fact]
    public async Task RegisterAsync_ReturnsError_WhenUsernameExists()
    {
        var (authService, userManager, roleManager) = BuildAuthFixture();
        await roleManager.CreateAsync(new IdentityRole("Reader"));

        var existing = new IdentityUser { UserName = "reader1" };
        await userManager.CreateAsync(existing, "Reader123!");

        var (success, error) = await authService.RegisterAsync(
            new RegisterRequest
            {
                UserName = "reader1",
                Password = "Reader123!",
                Role = "Reader"
            },
            CancellationToken.None);

        Assert.False(success);
        Assert.Contains("already exists", error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenCredentialsAreValid()
    {
        var (authService, userManager, roleManager) = BuildAuthFixture();
        await roleManager.CreateAsync(new IdentityRole("Editor"));

        var user = new IdentityUser { UserName = "editor1" };
        await userManager.CreateAsync(user, "Editor123!");
        await userManager.AddToRoleAsync(user, "Editor");

        var (success, error, response) = await authService.LoginAsync(
            new LoginRequest { UserName = "editor1", Password = "Editor123!" },
            CancellationToken.None);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response!.Token));
        Assert.Equal("Editor", response.Role);
    }

    [Fact]
    public async Task LoginAsync_ReturnsError_WhenPasswordIsInvalid()
    {
        var (authService, userManager, roleManager) = BuildAuthFixture();
        await roleManager.CreateAsync(new IdentityRole("Reader"));

        var user = new IdentityUser { UserName = "reader1" };
        await userManager.CreateAsync(user, "Reader123!");
        await userManager.AddToRoleAsync(user, "Reader");

        var (success, error, response) = await authService.LoginAsync(
            new LoginRequest { UserName = "reader1", Password = "WrongPassword!" },
            CancellationToken.None);

        Assert.False(success);
        Assert.Contains("Invalid username or password.", error);
        Assert.Null(response);
    }

    private static (AuthService AuthService, UserManager<IdentityUser> UserManager, RoleManager<IdentityRole> RoleManager) BuildAuthFixture()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "Patients.Api.Tests",
            ["Jwt:Audience"] = "Patients.Frontend.Tests",
            ["Jwt:Key"] = "PatientsApiTestsSecretKeyForJwt987654321",
            ["Jwt:ExpiresMinutes"] = "30"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"auth-tests-{Guid.NewGuid()}"));
        services
            .AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var authService = new AuthService(userManager, configuration);

        return (authService, userManager, roleManager);
    }
}
