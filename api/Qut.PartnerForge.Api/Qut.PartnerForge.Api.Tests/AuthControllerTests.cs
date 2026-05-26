using Qut.PartnerForge.Api.Controllers;
using Qut.PartnerForge.Api.Data;
using Qut.PartnerForge.Api.Interfaces;
using Qut.PartnerForge.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Qut.PartnerForge.Api.Tests;

public class AuthControllerTests
{
    private const string TestEmail = "test@example.com";
    private const string TestPassword = "Password123!";

    [Fact]
    public async Task Login_ReturnsOkAndUpdatesLastLogin_WhenCredentialsAreValid()
    {
        await using var context = CreateContext();
        var role = new Role { Id = 1, Name = "admin" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = TestEmail,
            PasswordHash = "hashed-password",
            FullName = "Test User",
            RoleId = role.Id,
            Role = role,
            IsActive = true
        };
        context.Roles.Add(role);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var auth = new Mock<IAuthService>();
        auth.Setup(a => a.VerifyPassword(TestPassword, user.PasswordHash)).Returns(true);
        auth.Setup(a => a.GenerateJwtToken(It.Is<User>(u => u.Id == user.Id))).Returns("jwt-token");
        var controller = new AuthController(context, auth.Object);

        var response = await controller.Login(new LoginRequest(TestEmail, TestPassword));

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal("jwt-token", body.Token);
        Assert.Equal(TestEmail, body.Email);
        Assert.Equal("admin", body.Role);
        Assert.NotNull(user.LastLoginAt);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        await using var context = CreateContext();
        var role = new Role { Id = 1, Name = "admin" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = TestEmail,
            PasswordHash = "hashed-password",
            FullName = "Test User",
            RoleId = role.Id,
            Role = role,
            IsActive = true
        };
        context.Roles.Add(role);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var auth = new Mock<IAuthService>();
        auth.Setup(a => a.VerifyPassword(TestPassword, user.PasswordHash)).Returns(false);
        var controller = new AuthController(context, auth.Object);

        var response = await controller.Login(new LoginRequest(TestEmail, TestPassword));

        Assert.IsType<UnauthorizedObjectResult>(response.Result);
        Assert.Null(user.LastLoginAt);
        auth.Verify(a => a.GenerateJwtToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenEmailAlreadyExists()
    {
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = TestEmail,
            PasswordHash = "hashed-password",
            FullName = "Existing User",
            RoleId = 1,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var controller = new AuthController(context, Mock.Of<IAuthService>());

        var response = await controller.Register(new RegisterUserRequest(
            TestEmail,
            TestPassword,
            "New User",
            "admin",
            FacultyId: null));

        Assert.IsType<ConflictObjectResult>(response.Result);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenRoleDoesNotExist()
    {
        await using var context = CreateContext();
        var controller = new AuthController(context, Mock.Of<IAuthService>());

        var response = await controller.Register(new RegisterUserRequest(
            TestEmail,
            TestPassword,
            "New User",
            "unknown",
            FacultyId: null));

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task Register_CreatesUserAndReturnsAuthResponse_WhenRequestIsValid()
    {
        await using var context = CreateContext();
        var role = new Role { Id = 1, Name = "course_organiser" };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var auth = new Mock<IAuthService>();
        auth.Setup(a => a.HashPassword(TestPassword)).Returns("hashed-password");
        auth.Setup(a => a.GenerateJwtToken(It.Is<User>(u => u.Email == TestEmail))).Returns("jwt-token");
        var controller = new AuthController(context, auth.Object);

        var response = await controller.Register(new RegisterUserRequest(
            TestEmail,
            TestPassword,
            "New User",
            role.Name,
            FacultyId: 3));

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<AuthResponse>(ok.Value);
        var savedUser = await context.Users.SingleAsync(u => u.Email == TestEmail);
        Assert.Equal("jwt-token", body.Token);
        Assert.Equal("New User", body.FullName);
        Assert.Equal(role.Name, body.Role);
        Assert.Equal(3, body.FacultyId);
        Assert.Equal("hashed-password", savedUser.PasswordHash);
        Assert.True(savedUser.IsActive);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
