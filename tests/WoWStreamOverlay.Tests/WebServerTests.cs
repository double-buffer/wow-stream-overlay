using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace WowStreamOverlay.Tests;

public class WebServerTests
{
    [Fact]
    public async Task GetStateReturnsCurrentGameState()
    {
        var state = new GameState
        {
            CurrentCharacterGuid = "Player-510-001577CC",
            Character = new CharacterProfile(
                "Shaigan",
                "Vol'jin",
                "voljin",
                "eu",
                CharacterClass.Warrior,
                CharacterSpecialization.ProtectionWarrior,
                CharacterRace.Human,
                90,
                301,
                "Guerrier",
                "Protection",
                "Humain"),
            MythicPlus = new MythicPlusState("Mists of Tirna Scithe", 11)
        };

        var server = WebServer.Create(state, "http://127.0.0.1:0");
        await server.StartAsync();

        try
        {
            var address = GetServerAddress(server);

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync($"{address}/api/state");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;

            Assert.Equal("Player-510-001577CC", root.GetProperty("currentCharacterGuid").GetString());

            var character = root.GetProperty("character");
            Assert.Equal("Shaigan", character.GetProperty("name").GetString());
            Assert.Equal("Vol'jin", character.GetProperty("realm").GetString());
            Assert.Equal(1, character.GetProperty("class").GetInt32());
            Assert.Equal(73, character.GetProperty("specialization").GetInt32());
            Assert.Equal(301, character.GetProperty("itemLevel").GetInt32());
            Assert.Equal("Guerrier", character.GetProperty("className").GetString());
            Assert.Equal("Protection", character.GetProperty("specializationName").GetString());

            var mythicPlus = root.GetProperty("mythicPlus");
            Assert.Equal("Mists of Tirna Scithe", mythicPlus.GetProperty("dungeonName").GetString());
            Assert.Equal(11, mythicPlus.GetProperty("level").GetInt32());
        }
        finally
        {
            await server.StopAsync();
            await server.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetOverlayReturnsConfiguredTemplateWithRuntime()
    {
        var templatePath = Path.Combine(Path.GetTempPath(), $"wow-stream-overlay-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(templatePath, "<html><body><span data-field=\"character.name\"></span></body></html>");

        var overlays = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["header"] = templatePath
        };

        var server = WebServer.Create(new GameState(), "http://127.0.0.1:0", overlays);
        await server.StartAsync();

        try
        {
            var address = GetServerAddress(server);

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync($"{address}/overlay/header");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

            var html = await response.Content.ReadAsStringAsync();

            Assert.Contains("data-field=\"character.name\"", html);
            Assert.Contains("fetch('/api/state')", html);
            Assert.True(html.IndexOf("<script>", StringComparison.Ordinal) < html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await server.StopAsync();
            await server.DisposeAsync();
            File.Delete(templatePath);
        }
    }

    private static string GetServerAddress(Microsoft.AspNetCore.Builder.WebApplication server)
    {
        var serverAddresses = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        return Assert.Single(serverAddresses!.Addresses);
    }
}
