using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Patients.Api.Features.Auth.Api;
using Patients.Api.Features.Auth.Application;
using Patients.Api.Features.Auth.Contracts;

namespace Patients.Api.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_ReturnsCreated_WhenSuccessful()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null));

        var controller = new AuthController(mock.Object);
        var request = new RegisterRequest { UserName = "newuser", Password = "Pass123!", Role = "Reader" };

        var result = await controller.Register(request, CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status201Created, status.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenUserAlreadyExists()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Username already exists."));

        var controller = new AuthController(mock.Object);
        var request = new RegisterRequest { UserName = "existing", Password = "Pass123!", Role = "Reader" };

        var result = await controller.Register(request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Invalid username or password.", null as AuthResponse));

        var controller = new AuthController(mock.Object);
        var request = new LoginRequest { UserName = "wrong", Password = "wrong" };

        var result = await controller.Login(request, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsOk_WithToken_WhenSuccessful()
    {
        var response = new AuthResponse
        {
            Token = "token",
            UserName = "reader",
            Role = "Reader",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        };

        var mock = new Mock<IAuthService>();
        mock.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, response));

        var controller = new AuthController(mock.Object);
        var request = new LoginRequest { UserName = "reader", Password = "Reader123!" };

        var result = await controller.Login(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal("reader", payload.UserName);
        Assert.Equal("Reader", payload.Role);
    }
}
