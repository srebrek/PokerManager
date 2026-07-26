using Contracts.IntegrationEvents.Identity;
using Identity.Abstractions;
using Identity.Features.Register;
using Identity.Infrastructure;
using NSubstitute;
using Shared.Domain;
using Shouldly;

namespace Identity.UnitTests.Features;

public sealed class RegisterCommandHandlerTests
{
    private readonly IUserAccountService _userAccountService = Substitute.For<IUserAccountService>();

    [Fact]
    public async Task Handle_RegistrationSucceeds_ReturnsUserRegisteredEvent()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        RegisterCommand command = new("new-user@example.com", "Password123!");
        _userAccountService
            .RegisterAsync(command.Email, command.Password)
            .Returns(Result.Success(userId));

        RegisterCommandHandler handler = new(_userAccountService);

        // Act
        (Result result, UserRegisteredIntegrationEvent? integrationEvent) = await handler.Handle(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        integrationEvent.ShouldNotBeNull();
        integrationEvent.UserId.ShouldBe(userId);
        integrationEvent.Email.ShouldBe(command.Email);
    }

    [Fact]
    public async Task Handle_RegistrationFails_DoesNotReturnAnyEvent()
    {
        // Arrange
        RegisterCommand command = new("duplicate@example.com", "Password123!");
        _userAccountService
            .RegisterAsync(command.Email, command.Password)
            .Returns(Result.Failure<Guid>(IdentityErrors.DuplicateEmail));

        RegisterCommandHandler handler = new(_userAccountService);

        // Act
        (Result result, UserRegisteredIntegrationEvent? integrationEvent) = await handler.Handle(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(IdentityErrors.DuplicateEmail);
        integrationEvent.ShouldBeNull();
    }
}
