using System.Text.Json;
using Contracts.Api.Gameplay;
using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Gameplay;

public sealed class CreateGameIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task CreateGame_EmptyHostName_IsRejectedByValidatorBeforeHandler()
    {
        // Arrange
        CreateGameRequest request = new(string.Empty);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.CreateGame,
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Proves the FluentValidation pipeline short-circuited, NOT the domain.
        // DO NOT REPEAT THIS ASSERT
        using JsonDocument problem = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(CancellationToken));
        JsonElement root = problem.RootElement;
        root.GetProperty("errorCode").GetString().ShouldBe("Validation.General");
        root.TryGetProperty("errors", out JsonElement errors).ShouldBeTrue();
        errors.TryGetProperty(nameof(CreateGameRequest.HostName), out _).ShouldBeTrue();

        await ExecuteWithContextAsync<GameplayDbContext>(async context =>
        {
            bool gameExists = await context.Games.AnyAsync(CancellationToken);
            gameExists.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task CreateGame_ValidHostName_ReturnsCreated()
    {
        // Arrange
        CreateGameRequest request = new("TestHostName");

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.CreateGame,
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        CreateGameResponse? body = await response.Content.ReadFromJsonAsync<CreateGameResponse>(CancellationToken);
        body.ShouldNotBeNull();
        body.GameId.ShouldNotBe(Guid.Empty);
        body.ParticipantId.ShouldNotBe(Guid.Empty);

        response.Headers.Location.ShouldNotBeNull();
        using HttpResponseMessage getGameStateResponse = await Client.GetAsync(
            response.Headers.Location,
            CancellationToken);

        getGameStateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        GameStateResponse? gameStateResponse =
            await getGameStateResponse.Content.ReadFromJsonAsync<GameStateResponse>(CancellationToken);
        gameStateResponse.ShouldNotBeNull();
        gameStateResponse.GameId.ShouldBe(body.GameId);
        gameStateResponse.Participants.ShouldHaveSingleItem().Id.ShouldBe(body.ParticipantId);
    }
}
