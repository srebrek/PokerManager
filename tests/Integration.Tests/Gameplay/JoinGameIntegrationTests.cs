using Contracts.Api.Gameplay;
using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests.Gameplay;

public sealed class JoinGameIntegrationTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task JoinGame_ValidRequest_ReturnsOk()
    {
        // Arrange
        Game game = Game.Create(
            "TestHostName",
            ChipsStack.Create(1000).Value,
            ChipsStack.Create(10).Value,
            ChipsStack.Create(20).Value).Value;

        await ExecuteWithContextAsync<GameplayDbContext>(async context =>
        {
            context.Games.Add(game);
            await context.SaveChangesAsync(CancellationToken);
        });

        JoinGameRequest request = new("TestParticipantName", game.JoinCode.Value);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.JoinGame,
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        JoinGameResponse? body = await response.Content.ReadFromJsonAsync<JoinGameResponse>(CancellationToken);
        body.ShouldNotBeNull();
        body.GameId.ShouldBe(game.Id.Value);

        await ExecuteWithContextAsync<GameplayDbContext>(async context =>
        {
            Game? retrievedGame = await context.Games
                .Include(g => g.Participants)
                .SingleOrDefaultAsync(g => g.Id == GameId.From(body.GameId), CancellationToken);

            retrievedGame.ShouldNotBeNull();
            retrievedGame.Participants.Any(p => p.Id.Value == body.ParticipantId).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task JoinGame_GameDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        JoinGameRequest request = new("TestParticipantName", "123456");

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.JoinGame,
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task JoinGame_EndedStatus_ReturnsConflict()
    {
        // Arrange
        Game game = Game.Create(
            "TestHostName",
            ChipsStack.Create(1000).Value,
            ChipsStack.Create(10).Value,
            ChipsStack.Create(20).Value).Value;

        game.Join("TestParticipantName1", ChipsStack.Create(1000).Value);
        game.Start();
        game.End();

        await ExecuteWithContextAsync<GameplayDbContext>(async context =>
        {
            context.Games.Add(game);
            await context.SaveChangesAsync(CancellationToken);
        });

        JoinGameRequest request = new("TestParticipantName2", game.JoinCode.Value);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.JoinGame,
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        await ExecuteWithContextAsync<GameplayDbContext>(async context =>
        {
            Game? retrievedGame = await context.Games
                .Include(g => g.Participants)
                .SingleOrDefaultAsync(g => g.Id == game.Id, CancellationToken);

            retrievedGame.ShouldNotBeNull();
            retrievedGame.Participants.Any(p => p.Name == request.ParticipantName).ShouldBeFalse();
        });
    }
}
