using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    private static readonly GameStateJsonContext JsonContext = new(JsonOptions);
    private static readonly GameStateJsonContext IndentedJsonContext = new(IndentedJsonOptions);

    /// <summary>
    /// Serializes a game state to compact JSON.
    /// </summary>
    public static string Serialize(GameState state)
    {
        return JsonSerializer.Serialize(state, JsonContext.GameState);
    }

    /// <summary>
    /// Serializes a game state to a stream.
    /// </summary>
    public static Task SerializeAsync(Stream stream, GameState state, bool indented = false, CancellationToken cancellationToken = default)
    {
        var context = indented ? IndentedJsonContext : JsonContext;
        return JsonSerializer.SerializeAsync(stream, state, context.GameState, cancellationToken);
    }

    /// <summary>
    /// Deserializes a game state from a stream.
    /// </summary>
    public static ValueTask<GameState?> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return JsonSerializer.DeserializeAsync(stream, JsonContext.GameState, cancellationToken);
    }
}

[JsonSerializable(typeof(GameState))]
internal partial class GameStateJsonContext : JsonSerializerContext
{
}
