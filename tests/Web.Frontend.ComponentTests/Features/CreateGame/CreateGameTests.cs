using Contracts.Api.Gameplay;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Web.Frontend.Common.Http;
using Web.Frontend.ComponentTests.Common;
using Web.Frontend.Features.CreateGame;

namespace Web.Frontend.ComponentTests.Features.CreateGame;

public sealed class CreateGameTests : MudBunitContext
{
    private readonly IGameplayApi _api = Substitute.For<IGameplayApi>();

    public CreateGameTests()
    {
        Services.AddSingleton(_api);
    }

    [Fact]
    public async Task CreateGame_InvalidSubmit_DoesNotCallApi()
    {
        // Arrange
        IRenderedComponent<CreateGameForm> cut = Render<CreateGameForm>();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await _api.DidNotReceive().CreateGameAsync(Arg.Any<CreateGameRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateGame_ValidSubmit_CallsApiAndTriggersCallback()
    {
        // Arrange
        CreateGameForm.GameCreated expectedGameCreated = new(Guid.NewGuid(), Guid.NewGuid());

        CreateGameRequest expectedCreateGameRequest = new("TestHostName");
        CreateGameResponse apiCreateGameResponse = new(expectedGameCreated.GameId, expectedGameCreated.ParticipantId);
        _api.CreateGameAsync(expectedCreateGameRequest, Arg.Any<CancellationToken>())
            .Returns(GameplayResult.Success(apiCreateGameResponse));

        CreateGameForm.GameCreated actualGameCreated = default;
        IRenderedComponent<CreateGameForm> cut = Render<CreateGameForm>(parameters => parameters
            .Add(form => form.OnCreated, gc => actualGameCreated = gc));

        // Act
        await cut.Find("input").ChangeAsync("TestHostName");
        await cut.Find("form").SubmitAsync();

        // Assert
        await _api.Received(1).CreateGameAsync(expectedCreateGameRequest, Arg.Any<CancellationToken>());
        actualGameCreated.ShouldBe(expectedGameCreated);
    }

    [Fact]
    public async Task CreateGame_ApiReturnsError_DisplaysErrorAndDoesNotTriggerCallback()
    {
        // Arrange
        string expectedErrorMessage = "Server error. Try again.";
        _api.CreateGameAsync(Arg.Any<CreateGameRequest>(), Arg.Any<CancellationToken>())
            .Returns(GameplayResult.Failure<CreateGameResponse>(expectedErrorMessage, GameplayErrorKind.Server));

        bool callbackCalled = false;
        IRenderedComponent<CreateGameForm> cut = Render<CreateGameForm>(parameters => parameters
            .Add(form => form.OnCreated, gc => callbackCalled = true));

        // Act
        await cut.Find("input").ChangeAsync("TestHostName");
        await cut.Find("form").SubmitAsync();

        // Assert
        IRenderedComponent<MudAlert> alert = cut.FindComponent<MudAlert>();
        alert.Markup.ShouldContain(expectedErrorMessage);
        callbackCalled.ShouldBe(false);
    }
}
