using Contracts.Api.Gameplay;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Web.Frontend.Common.Http;
using Web.Frontend.ComponentTests.Common;
using Web.Frontend.Features.FindGame;

namespace Web.Frontend.ComponentTests.Features.FindGame;

public sealed class FindGameTests : MudBunitContext
{
    private readonly IGameplayApi _api = Substitute.For<IGameplayApi>();

    public FindGameTests()
    {
        Services.AddSingleton(_api);
    }

    [Fact]
    public async Task FindGame_InvalidSubmit_DoesNotCallApi()
    {
        // Arrange
        IRenderedComponent<FindGameForm> cut = Render<FindGameForm>();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await _api.DidNotReceive().GetGameByJoinCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindGame_ValidSubmit_CallsApiAndTriggersCallback()
    {
        // Arrange
        FindGameForm.GameFound expectedGameFound = new(Guid.NewGuid());

        _api.GetGameByJoinCodeAsync("123456", Arg.Any<CancellationToken>())
            .Returns(GameplayResult.Success(new GameLookupResponse(expectedGameFound.GameId)));

        FindGameForm.GameFound actualGameFound = default;
        IRenderedComponent<FindGameForm> cut = Render<FindGameForm>(parameters => parameters
            .Add(form => form.OnFound, gf => actualGameFound = gf));

        // Act
        IRenderedComponent<MudTextField<string>> joinCode = cut.FindComponents<MudTextField<string>>()
            .First(c => c.Instance.Label is "Join Code");
        await joinCode.Find("input").ChangeAsync("123456");
        await cut.Find("form").SubmitAsync();

        // Assert
        await _api.Received(1).GetGameByJoinCodeAsync("123456", Arg.Any<CancellationToken>());
        actualGameFound.ShouldBe(expectedGameFound);
    }

    [Fact]
    public async Task FindGame_ApiReturnsError_DisplaysErrorAndDoesNotTriggerCallback()
    {
        // Arrange
        string expectedErrorMessage = "Game not found.";
        _api.GetGameByJoinCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(GameplayResult.Failure<GameLookupResponse>(expectedErrorMessage, GameplayErrorKind.NotFound));

        bool callbackCalled = false;
        IRenderedComponent<FindGameForm> cut = Render<FindGameForm>(parameters => parameters
            .Add(form => form.OnFound, gf => callbackCalled = true));

        // Act
        IRenderedComponent<MudTextField<string>> joinCode = cut.FindComponents<MudTextField<string>>()
            .First(c => c.Instance.Label is "Join Code");
        await joinCode.Find("input").ChangeAsync("123456");
        await cut.Find("form").SubmitAsync();

        // Assert
        IRenderedComponent<MudAlert> alert = cut.FindComponent<MudAlert>();
        alert.Markup.ShouldContain(expectedErrorMessage);
        callbackCalled.ShouldBe(false);
    }
}
