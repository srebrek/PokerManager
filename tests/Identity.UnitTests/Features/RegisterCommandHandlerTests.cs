using Identity.Abstractions;
using Identity.Events;
using Identity.Features.Register;
using Identity.Infrastructure;
using NSubstitute;
using Shared.Domain;
using Shouldly;
using Wolverine;

namespace Identity.UnitTests.Features;

public sealed class RegisterCommandHandlerTests
{
    private readonly IUserAccountService _userAccountService = Substitute.For<IUserAccountService>();
    private readonly IMessageContext _messageContext = Substitute.For<IMessageContext>();

    [Fact]
    public async Task Handle_WhenRegistrationSucceeds_ShouldPublishUserRegisteredEvent()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        RegisterCommand command = new("new-user@example.com", "Password123!");
        _userAccountService
            .RegisterAsync(command.Email, command.Password)
            .Returns(Result.Success(userId));

        RegisterCommandHandler handler = new(_userAccountService);

        // Act
        Result result = await handler.Handle(command, _messageContext);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _messageContext.Received(1).PublishAsync(
            Arg.Is<UserRegisteredIntegrationEvent>(e => e.UserId == userId && e.Email == command.Email));
    }

    [Fact]
    public async Task Handle_WhenRegistrationFails_ShouldNotPublishAnyEvent()
    {
        // Arrange
        RegisterCommand command = new("duplicate@example.com", "Password123!");
        _userAccountService
            .RegisterAsync(command.Email, command.Password)
            .Returns(Result.Failure<Guid>(IdentityErrors.DuplicateEmail));

        RegisterCommandHandler handler = new(_userAccountService);

        // Act
        Result result = await handler.Handle(command, _messageContext);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(IdentityErrors.DuplicateEmail);
        await _messageContext.DidNotReceiveWithAnyArgs().PublishAsync<UserRegisteredIntegrationEvent>(default!);
    }
}
