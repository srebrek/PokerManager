using Contracts.Api.Gameplay;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Web.Frontend.Common.Http;
using Web.Frontend.Component.Tests.Common;
using Web.Frontend.Features.JoinGame;

namespace Web.Frontend.Component.Tests.Features.JoinGame;

public sealed class JoinGameTests : MudBunitContext
{
    private readonly IGameplayApi _api = Substitute.For<IGameplayApi>();

    public JoinGameTests()
    {
        Services.AddSingleton(_api);
    }

    [Fact]
    public async Task JoinGame_InvalidSubmit_DoesNotCallApi()
    {
        // Arrange
        IRenderedComponent<JoinGameForm> cut = Render<JoinGameForm>();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await _api.DidNotReceive().JoinGameAsync(Arg.Any<JoinGameRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinGame_ValidSubmit_CallsApiAndTriggersCallback()
    {
        // Arrange
        JoinGameForm.GameJoined expectedGameJoined = new(Guid.NewGuid(), Guid.NewGuid());

        JoinGameRequest expectedJoinGameRequest = new("TestParticipantName", "123456");
        JoinGameResponse apiJoinGameResponse = new(expectedGameJoined.GameId, expectedGameJoined.ParticipantId);
        _api.JoinGameAsync(expectedJoinGameRequest, Arg.Any<CancellationToken>())
            .Returns(GameplayResult.Success(apiJoinGameResponse));

        JoinGameForm.GameJoined actualGameJoined = default;
        IRenderedComponent<JoinGameForm> cut = Render<JoinGameForm>(parameters => parameters
            .Add(form => form.OnJoined, gj => actualGameJoined = gj));

        // Act
        IReadOnlyList<IRenderedComponent<MudTextField<string>>> inputs = cut.FindComponents<MudTextField<string>>();
        await inputs.First(c => c.Instance.Label is "Your Name").Find("input").ChangeAsync("TestParticipantName");
        await inputs.First(c => c.Instance.Label is "Join Code").Find("input").ChangeAsync("123456");
        await cut.Find("form").SubmitAsync();

        // Assert
        await _api.Received(1).JoinGameAsync(expectedJoinGameRequest, Arg.Any<CancellationToken>());
        actualGameJoined.ShouldBe(expectedGameJoined);
    }

    [Fact]
    public async Task JoinGame_ApiReturnsError_DisplaysErrorAndDoesNotTriggerCallback()
    {
        // Arrange
        string expectedErrorMessage = "Server error. Try again.";
        _api.JoinGameAsync(Arg.Any<JoinGameRequest>(), Arg.Any<CancellationToken>())
            .Returns(GameplayResult.Failure<JoinGameResponse>(expectedErrorMessage));

        bool callbackCalled = false;
        IRenderedComponent<JoinGameForm> cut = Render<JoinGameForm>(parameters => parameters
            .Add(form => form.OnJoined, gj => callbackCalled = true));

        // Act
        IReadOnlyList<IRenderedComponent<MudTextField<string>>> inputs = cut.FindComponents<MudTextField<string>>();
        await inputs.First(c => c.Instance.Label is "Your Name").Find("input").ChangeAsync("TestParticipantName");
        await inputs.First(c => c.Instance.Label is "Join Code").Find("input").ChangeAsync("123456");
        await cut.Find("form").SubmitAsync();

        // Assert
        IRenderedComponent<MudAlert> alert = cut.FindComponent<MudAlert>();
        alert.Markup.ShouldContain(expectedErrorMessage);
        callbackCalled.ShouldBe(false);
    }
}
