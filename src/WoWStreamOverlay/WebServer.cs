using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace WowStreamOverlay;

/// <summary>
/// Creates the local HTTP server exposed by the application.
/// </summary>
public static class WebServer
{
    public const string DefaultUrl = "http://127.0.0.1:37231";

    public static WebApplication Create(GameState state, string? url = null)
    {
        var builder = WebApplication.CreateSlimBuilder([]);
        builder.WebHost.UseUrls(url ?? DefaultUrl);

        var server = builder.Build();
        server.MapGet("/api/state", () => Results.Text(GameStateSerializer.Serialize(state), "application/json"));

        return server;
    }
}
