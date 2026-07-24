using System.ComponentModel.DataAnnotations;
using Contracts.Gameplay;

namespace Web.Frontend.Features.CreateGame;

internal sealed class CreateGameModel
{
    [Required]
    public string HostName { get; set; } = string.Empty;

    public CreateGameRequest ToRequest() => new(HostName);
}
