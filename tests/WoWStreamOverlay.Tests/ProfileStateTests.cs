using System.Text.Json;
using Xunit;

namespace WowStreamOverlay.Tests;

public class ProfileStateTests
{
    [Fact]
    public void GameStateSerializationIncludesMythicPlusScore()
    {
        var state = new GameState
        {
            Character = new CharacterProfile(
                "Shaigan",
                "Vol'jin",
                "voljin",
                "eu",
                CharacterClass.Warrior,
                CharacterSpecialization.ProtectionWarrior,
                CharacterRace.Human,
                90,
                305,
                "Guerrier",
                "Protection",
                "Humain",
                2081)
        };

        using var json = JsonDocument.Parse(GameStateSerializer.Serialize(state));
        var character = json.RootElement.GetProperty("character");

        Assert.Equal(305, character.GetProperty("itemLevel").GetInt32());
        Assert.Equal(2081, character.GetProperty("mythicPlusScore").GetInt32());
    }

    [Fact]
    public void OverlayRuntimeSupportsHidingIdleStateDuringActiveMythicPlus()
    {
        const string template = "<html><body><div data-visible-field=\"character.mythicPlusScore\" data-hidden-field=\"mythicPlus\"></div></body></html>";

        var html = OverlayRenderer.Render(template);

        Assert.Contains("[data-visible-field], [data-hidden-field]", html);
        Assert.Contains("element.dataset.hiddenField", html);
        Assert.Contains("element.hidden = !visible || hidden", html);
    }
}
