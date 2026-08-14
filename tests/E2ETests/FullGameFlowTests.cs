using System.Globalization;
using System.Net.Http.Json;
using Contracts.Api.Gameplay;
using Microsoft.Playwright;
using Shouldly;
using static Microsoft.Playwright.Assertions;

namespace E2ETests;

public sealed class FullGameFlowTests(AspireFixture fixture, ITestOutputHelper output)
    : AspireIntegrationTestBase(fixture, output)
{
    private const string HostName = "srebrekhost";
    private const string SmallBlindName = "srebreksb";
    private const string BigBlindName = "srebrekbb";

    private const int SmallBlind = 5;
    private const int BigBlind = 10;
    private const int StartingStack = 1000;

    private Guid _gameId;
    private Guid _handId;

    [Fact]
    public async Task ThreeParticipants_PlayHandToShowdown_SettleStacksAndFinishGame()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;

        IPage host = Page;
        string joinCode = await CreateGameAsync(host);
        _gameId = Guid.Parse(new Uri(host.Url).Segments[^1], CultureInfo.InvariantCulture);
        IPage alice = await JoinGameAsync(joinCode, SmallBlindName);
        IPage bob = await JoinGameAsync(joinCode, BigBlindName);

        await RefreshAsync(host);
        await Expect(host.GetByTestId("participant-row")).ToHaveCountAsync(3);

        await ClickAndWaitForResponseAsync(host, "start-hand", GameplayRoutes.StartHandFor(_gameId).ToString());
        await Expect(
            host.GetByTestId("pot")).ToHaveTextAsync((SmallBlind + BigBlind).ToString(CultureInfo.InvariantCulture));
        await Expect(host.GetByTestId("street")).ToHaveTextAsync("PreFlop");

        await RefreshIntoHandAsync(alice);
        await RefreshIntoHandAsync(bob);

        using HttpClient client = CreateApiClient();
        GameStateResponse? gameState =
            await client.GetFromJsonAsync<GameStateResponse>(GameplayRoutes.GameStateFor(_gameId), ct);
        _handId = gameState?.CurrentHandId ?? throw new InvalidOperationException("Hand did not start.");

        await ActAsync(host, "action-call");
        await ActAsync(alice, "action-call");
        await ActAsync(bob, "action-call");

        await RefreshAsync(bob);
        await Expect(bob.GetByTestId("pot")).ToHaveTextAsync((3 * BigBlind).ToString(CultureInfo.InvariantCulture));
        await Expect(bob.GetByTestId("street")).ToHaveTextAsync("Flop");

        await CheckAroundAsync(alice, bob, host, "Turn");
        await CheckAroundAsync(alice, bob, host, "River");
        await CheckAroundAsync(alice, bob, host, "Finished");

        await host.GetByTestId("seat-card").Filter(new LocatorFilterOptions { HasText = SmallBlindName })
            .ClickAsync();
        await ClickAndWaitForResponseAsync(host, "finish-hand", GameplayRoutes.FinishHandFor(_handId).ToString());

        await RefreshAsync(host);
        await Expect(host.GetByTestId("no-hand")).ToBeVisibleAsync();
        await ExpectChipsAsync(host, SmallBlindName, StartingStack + (2 * BigBlind));
        await ExpectChipsAsync(host, BigBlindName, StartingStack - BigBlind);
        await ExpectChipsAsync(host, HostName, StartingStack - BigBlind);

        await ClickAndWaitForResponseAsync(host, "finish-game", GameplayRoutes.FinishGameFor(_gameId).ToString());

        // TODO: add FE assert when finished is displayed
        GameStateResponse? state = await client.GetFromJsonAsync<GameStateResponse>(
            GameplayRoutes.GameStateFor(_gameId), ct);

        state.ShouldNotBeNull();
        state.IsFinished.ShouldBeTrue();
    }

    private async Task<string> CreateGameAsync(IPage page)
    {
        await page.GotoAsync(FrontendBaseUri.ToString() + "games/setup");
        await page.GetByLabel("Your Name").FillAsync(HostName);
        await ClickAndWaitForResponseAsync(page, "create-game-submit", GameplayRoutes.CreateGame);

        ILocator joinCode = page.GetByTestId("join-code");
        await Expect(joinCode).ToBeVisibleAsync();

        return await joinCode.TextContentAsync()
            ?? throw new InvalidOperationException("Join code is missing from the lobby.");
    }

    private async Task<IPage> JoinGameAsync(string joinCode, string playerName)
    {
        IPage page = await NewPageAsync();
        await page.GotoAsync(FrontendBaseUri.ToString());
        await page.GetByLabel("Join Code").FillAsync(joinCode);
        await ClickAndWaitForResponseAsync(page, "find-game-submit", GameplayRoutes.GetGameByJoinCode);
        await Expect(page.GetByTestId("join-code")).ToBeVisibleAsync();
        await page.GetByLabel("Your Name").FillAsync(playerName);
        await ClickAndWaitForResponseAsync(
            page, "join-as-new-player", GameplayRoutes.AddParticipantFor(_gameId).ToString());
        return page;
    }

    private static Task RefreshAsync(IPage page) => page.GetByTestId("refresh").ClickAsync();

    private static async Task ClickAndWaitForResponseAsync(IPage page, string testId, string urlFragment)
    {
        IResponse response = await page.RunAndWaitForResponseAsync(
            () => page.GetByTestId(testId).ClickAsync(),
            r => r.Url.Contains(urlFragment, StringComparison.Ordinal));

        string body = response.Ok ? string.Empty : await response.TextAsync();
        response.Ok.ShouldBeTrue($"{response.Request.Method} {response.Url} returned {response.Status}. {body}");
    }

    private static async Task RefreshIntoHandAsync(IPage page)
    {
        await RefreshAsync(page);
        await Expect(page.GetByTestId("hand-panel")).ToBeVisibleAsync();
    }

    private Task ActAsync(IPage page, string actionTestId) =>
        ClickAndWaitForResponseAsync(page, actionTestId, GameplayRoutes.RecordActionFor(_handId).ToString());

    private async Task CheckAroundAsync(IPage first, IPage second, IPage last, string expectedNextStreet)
    {
        await ActAsync(first, "action-check");
        await ActAsync(second, "action-check");
        await ActAsync(last, "action-check");

        await RefreshAsync(last);
        await Expect(last.GetByTestId("street")).ToHaveTextAsync(expectedNextStreet);
    }

    private static async Task ExpectChipsAsync(IPage page, string participantName, int expectedChips)
    {
        ILocator row = page.GetByTestId("participant-row")
            .Filter(new LocatorFilterOptions { HasText = participantName });

        await Expect(
            row.GetByTestId("participant-chips")).ToHaveTextAsync(expectedChips.ToString(CultureInfo.InvariantCulture));
    }
}
