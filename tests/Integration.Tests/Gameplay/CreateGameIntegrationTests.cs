using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Contracts.Api.Gameplay;
using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Integration.Tests.Gameplay;

public sealed class CreateGameIntegrationTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateGame_WithEmptyHostName_ShouldBeRejectedByValidatorBeforeHandler()
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

        using JsonDocument problem = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(CancellationToken));
        JsonElement root = problem.RootElement;

        // Proves the FluentValidation pipeline short-circuited, NOT the domain.
        // DO NOT REPEAT THIS ASSERT
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
    public async Task CreateGame_WithValidHostName_ShouldReturnCreated()
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
        response.Headers.Location.ToString().ShouldBe($"gameplay/games/{body.GameId}");

        await ExecuteWithContextAsync<GameplayDbContext>(async context =>
        {
            Game? game = await context.Games
                .Include(g => g.Participants)
                .SingleOrDefaultAsync(g => g.Id == GameId.From(body.GameId), CancellationToken);

            game.ShouldNotBeNull();
            game.Participants.Count.ShouldBe(1);
            Participant? participant = game.Participants.SingleOrDefault();
            participant.ShouldNotBeNull();
            participant.Id.ShouldBe(game.HostParticipantId);
        });
    }
}
