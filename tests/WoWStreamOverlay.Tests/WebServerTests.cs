using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WowStreamOverlay.CombatLog;
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

        var server = WebServer.Create(state, host: "127.0.0.1", port: 0);
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
            Assert.Equal("#C79C6E", character.GetProperty("classColor").GetString());

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
    public async Task EventsReturnsInitialStateAndSubsequentChanges()
    {
        var statePath = Path.Combine(Path.GetTempPath(), $"wow-stream-overlay-state-{Guid.NewGuid():N}.json");
        var characterCachePath = Path.Combine(Path.GetTempPath(), $"wow-stream-overlay-characters-{Guid.NewGuid():N}.json");
        var state = new GameState();
        var app = new WoWStreamOverlayApp(
            new CombatLogParser(),
            new CharacterCache(characterCachePath),
            null,
            state,
            new GameStateStore(statePath),
            TimeSpan.FromMinutes(1));

        var server = WebServer.Create(state, host: "127.0.0.1", port: 0);
        await server.StartAsync();

        try
        {
            var address = GetServerAddress(server);

            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{address}/events");
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            using (var initialJson = JsonDocument.Parse(await ReadSseDataAsync(reader)))
            {
                Assert.Equal(JsonValueKind.Null, initialJson.RootElement.GetProperty("mythicPlus").ValueKind);
            }

            await app.ProcessCombatLogLineAsync(
                "8/25/2026 09:00:00.0000  CHALLENGE_MODE_START,\"Antre de Nalorakk\",2825,999,9,[1,2,3,4]");

            using var updatedJson = JsonDocument.Parse(await ReadSseDataAsync(reader));
            var mythicPlus = updatedJson.RootElement.GetProperty("mythicPlus");

            Assert.Equal("Antre de Nalorakk", mythicPlus.GetProperty("dungeonName").GetString());
            Assert.Equal(9, mythicPlus.GetProperty("level").GetInt32());
        }
        finally
        {
            await server.StopAsync();
            await server.DisposeAsync();
            File.Delete(statePath);
            File.Delete(characterCachePath);
        }
    }

    [Fact]
    public async Task EventsEndsWhenApplicationStops()
    {
        using var applicationStopping = new CancellationTokenSource();
        var server = WebServer.Create(
            new GameState(),
            host: "127.0.0.1",
            port: 0,
            applicationStopping: applicationStopping.Token);
        await server.StartAsync();

        try
        {
            var address = GetServerAddress(server);

            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{address}/events");
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            await ReadSseDataAsync(reader);
            applicationStopping.Cancel();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            while (await reader.ReadLineAsync(timeout.Token) is not null)
            {
            }
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
        await File.WriteAllTextAsync(
            templatePath,
            "<html><body><span data-field=\"character.name\" data-color-field=\"character.classColor\"></span></body></html>");

        var overlays = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["header"] = templatePath
        };

        var server = WebServer.Create(new GameState(), host: "127.0.0.1", port: 0, overlays: overlays);
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
            Assert.Contains("data-color-field=\"character.classColor\"", html);
            Assert.Contains("new EventSource('/events')", html);
            Assert.DoesNotContain("fetch('/api/state')", html);
            Assert.Contains("[data-color-field]", html);
            Assert.True(html.IndexOf("<script>", StringComparison.Ordinal) < html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await server.StopAsync();
            await server.DisposeAsync();
            File.Delete(templatePath);
        }
    }

    private static async Task<string> ReadSseDataAsync(StreamReader reader)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (true)
        {
            var line = await reader.ReadLineAsync(timeout.Token);

            if (line is null)
            {
                throw new InvalidOperationException("The SSE connection ended before an event was received.");
            }

            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                return line[6..];
            }
        }
    }

    private static string GetServerAddress(Microsoft.AspNetCore.Builder.WebApplication server)
    {
        var serverAddresses = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        return Assert.Single(serverAddresses!.Addresses);
    }
}
