namespace WowStreamOverlay;

/// <summary>
/// Persists the current application state.
/// </summary>
public sealed class GameStateStore
{
    private readonly string _path;

    public GameStateStore(string path)
    {
        _path = path;
    }

    public async Task<GameState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return new GameState();
        }

        await using var stream = File.OpenRead(_path);
        var state = await GameStateJson.DeserializeAsync(stream, cancellationToken) ?? new GameState();

        state.MythicPlus = null;
        return state;
    }

    public async Task SaveAsync(GameState state, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{_path}.tmp";

        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await GameStateJson.SerializeAsync(stream, state, indented: true, cancellationToken: cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, _path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
