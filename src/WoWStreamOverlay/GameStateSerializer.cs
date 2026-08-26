using System.Text.Encodings.Web;
using System.Text.Json;

namespace WowStreamOverlay;

/// <summary>
/// Serializes and deserializes the current game state.
/// </summary>
public static class GameStateSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions IndentedJsonOptions = new(JsonOptions)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Serializes a game state to compact JSON.
    /// </summary>
    public static string Serialize(GameState state)
    {
        return JsonSerializer.Serialize(state, JsonOptions);
    }

    /// <summary>
    /// Serializes a game state to a stream.
    /// </summary>
    public static Task SerializeAsync(Stream stream, GameState state, bool indented = false, CancellationToken cancellationToken = default)
    {
        var options = indented ? IndentedJsonOptions : JsonOptions;
        return JsonSerializer.SerializeAsync(stream, state, options, cancellationToken);
    }

    /// <summary>
    /// Deserializes a game state from a stream.
    /// </summary>
    public static ValueTask<GameState?> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return JsonSerializer.DeserializeAsync<GameState>(stream, JsonOptions, cancellationToken);
    }
}
