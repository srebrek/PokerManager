using Contracts.Gameplay;
using Microsoft.AspNetCore.Components;
using NSubstitute;
using Shouldly;
using Web.Frontend.Common.Http;
using Web.Frontend.Component.Tests.Common;
using CreateGamePage = Web.Frontend.Features.CreateGame.CreateGame;

namespace Web.Frontend.Component.Tests.Features.CreateGame;

public sealed class CreateGameTests : MudBunitContext
{
    private readonly IGameplayApi _api = Substitute.For<IGameplayApi>();

    public CreateGameTests()
    {
        Services.AddSingleton(_api);
    }

    [Fact]
    public async Task CreateGame_WithEmptySubmit_ShouldNotCallApi()
    {
        // Arrange
        IRenderedComponent<CreateGamePage> cut = Render<CreateGamePage>();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await _api.DidNotReceive().CreateGameAsync(Arg.Any<CreateGameRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateGame_WithValidSubmit_ShouldCallApiAndNavigateOnSuccess()
    {
        // Arrange
        Guid gameId = Guid.NewGuid();
        IRenderedComponent<CreateGamePage> cut = Render<CreateGamePage>();
        _api.CreateGameAsync(Arg.Any<CreateGameRequest>(), Arg.Any<CancellationToken>())
            .Returns(GameplayResult.Success(new CreateGameResponse(gameId, "213769")));

        // Act
        await cut.Find("input").ChangeAsync("srebrek");
        await cut.Find("form").SubmitAsync();

        // Assert
        await _api.Received(1).CreateGameAsync(Arg.Is<CreateGameRequest>(r => r.HostName == "srebrek"), Arg.Any<CancellationToken>());
        BunitNavigationManager nav = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith($"/games/{gameId}");

    }

    [Fact]
    public async Task CreateGame_WithValidSubmit_ShouldShowErrorOnFailure()
    {
        // Arrange
        _api.CreateGameAsync(Arg.Any<CreateGameRequest>(), Arg.Any<CancellationToken>())
            .Returns(GameplayResult.Failure<CreateGameResponse>("Server error. Try again."));
        IRenderedComponent<CreateGamePage> cut = Render<CreateGamePage>();

        // Act
        await cut.Find("input").ChangeAsync("srebrek");
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Server error. Try again.");
    }
}
