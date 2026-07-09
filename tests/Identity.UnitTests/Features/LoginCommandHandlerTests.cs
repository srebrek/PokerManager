using Identity.Abstractions;
using Identity.Features.Login;
using Identity.Infrastructure;
using NSubstitute;
using Shared.Domain;
using Shouldly;

namespace Identity.UnitTests.Features;

public sealed class LoginCommandHandlerTests
{
    private readonly IUserAccountService _userAccountService = Substitute.For<IUserAccountService>();

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        LoginCommand command = new("user@example.com", "Password123!", RememberMe: false);
        _userAccountService
            .LoginAsync(command.Email, command.Password, command.RememberMe)
            .Returns(Result.Success());

        LoginCommandHandler handler = new(_userAccountService);

        // Act
        Result result = await handler.Handle(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidCredentials_ShouldReturnFailure()
    {
        // Arrange
        LoginCommand command = new("user@example.com", "WrongPassword!", RememberMe: false);
        _userAccountService
            .LoginAsync(command.Email, command.Password, command.RememberMe)
            .Returns(Result.Failure(IdentityErrors.InvalidCredentials));

        LoginCommandHandler handler = new(_userAccountService);

        // Act
        Result result = await handler.Handle(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(IdentityErrors.InvalidCredentials);
    }
}
