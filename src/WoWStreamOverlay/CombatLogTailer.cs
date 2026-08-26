using System.Runtime.CompilerServices;
using System.Text;

namespace WowStreamOverlay;

/// <summary>
/// Follows the active World of Warcraft combat log and yields complete lines as they are written.
/// </summary>
public sealed class CombatLogTailer
{
    private const string LogFilePattern = "WoWCombatLog*.txt";
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private readonly string _logsPath;

    public CombatLogTailer(string logsPath)
    {
        _logsPath = logsPath;
    }

    public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var currentPath = FindLatestLogFile();
        var reader = currentPath is null ? null : OpenReader(currentPath, seekToEnd: true);
        var buffer = new char[4096];
        var lineBuffer = new StringBuilder();

        if (currentPath is not null)
        {
            Console.WriteLine($"Following combat log: {Path.GetFileName(currentPath)}");
        }

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader is null)
                {
                    currentPath = FindLatestLogFile();

                    if (currentPath is null)
                    {
                        await Task.Delay(PollInterval, cancellationToken);
                        continue;
                    }

                    reader = OpenReader(currentPath, seekToEnd: false);
                    Console.WriteLine($"Following new combat log: {Path.GetFileName(currentPath)}");
                }

                var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);

                if (read > 0)
                {
                    var lineStart = 0;

                    for (var index = 0; index < read; index++)
                    {
                        if (buffer[index] != '\n')
                        {
                            continue;
                        }

                        lineBuffer.Append(buffer, lineStart, index - lineStart);

                        if (lineBuffer.Length > 0 && lineBuffer[lineBuffer.Length - 1] == '\r')
                        {
                            lineBuffer.Length--;
                        }

                        if (lineBuffer.Length > 0)
                        {
                            yield return lineBuffer.ToString();
                        }

                        lineBuffer.Clear();
                        lineStart = index + 1;
                    }

                    if (lineStart < read)
                    {
                        lineBuffer.Append(buffer, lineStart, read - lineStart);
                    }

                    continue;
                }

                var latestPath = FindLatestLogFile();

                if (latestPath is not null && !string.Equals(latestPath, currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    reader.Dispose();
                    reader = OpenReader(latestPath, seekToEnd: false);
                    currentPath = latestPath;
                    lineBuffer.Clear();
                    Console.WriteLine($"Following new combat log: {Path.GetFileName(currentPath)}");
                    continue;
                }

                await Task.Delay(PollInterval, cancellationToken);
            }
        }
        finally
        {
            reader?.Dispose();
        }
    }

    private string? FindLatestLogFile()
    {
        string? latestPath = null;
        var latestWriteTime = DateTime.MinValue;
        var latestCreationTime = DateTime.MinValue;

        foreach (var path in Directory.EnumerateFiles(_logsPath, LogFilePattern, SearchOption.TopDirectoryOnly))
        {
            var file = new FileInfo(path);

            if (!file.Exists)
            {
                continue;
            }

            if (latestPath is not null && file.LastWriteTimeUtc < latestWriteTime)
            {
                continue;
            }

            if (latestPath is not null && file.LastWriteTimeUtc == latestWriteTime && file.CreationTimeUtc <= latestCreationTime)
            {
                continue;
            }

            latestPath = file.FullName;
            latestWriteTime = file.LastWriteTimeUtc;
            latestCreationTime = file.CreationTimeUtc;
        }

        return latestPath;
    }

    private static StreamReader OpenReader(string path, bool seekToEnd)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan);

        if (seekToEnd)
        {
            stream.Seek(0, SeekOrigin.End);
        }

        return new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: false);
    }
}
