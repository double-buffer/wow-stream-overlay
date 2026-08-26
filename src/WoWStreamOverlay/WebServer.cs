using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace WowStreamOverlay;

/// <summary>
/// Creates the local HTTP server exposed by the application.
/// </summary>
public static class WebServer
{
    public const string DefaultHost = "127.0.0.1";
    public const int DefaultPort = 37231;

    public static WebApplication Create(
        GameState state,
        string? host = null,
        int? port = null,
        IReadOnlyDictionary<string, string>? overlays = null)
    {
        var builder = WebApplication.CreateSlimBuilder([]);
        builder.WebHost.UseUrls($"http://{host ?? DefaultHost}:{port ?? DefaultPort}");

        var server = builder.Build();
        server.MapGet("/api/state", () => Results.Text(GameStateSerializer.Serialize(state), "application/json"));

        server.MapGet("/overlay/{name}", async (string name, CancellationToken cancellationToken) =>
        {
            if (overlays is null || !overlays.TryGetValue(name, out var templatePath) || !File.Exists(templatePath))
            {
                return Results.NotFound();
            }

            var template = await File.ReadAllTextAsync(templatePath, cancellationToken);
            var html = OverlayRenderer.Render(template);

            return Results.Content(html, "text/html; charset=utf-8");
        });

        return server;
    }
}
