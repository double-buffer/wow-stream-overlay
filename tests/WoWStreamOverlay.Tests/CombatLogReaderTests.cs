using Xunit;

namespace WowStreamOverlay.Tests;

public class CombatLogReaderTests
{
    [Fact]
    public async Task FindLastPlayerObservedReturnsLastLocalPlayerFromLatestLog()
    {
        var logsPath = Path.Combine(Path.GetTempPath(), $"wow-stream-overlay-logs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(logsPath);

        try
        {
            var oldPath = Path.Combine(logsPath, "WoWCombatLog-old.txt");
            var latestPath = Path.Combine(logsPath, "WoWCombatLog-latest.txt");

            await File.WriteAllTextAsync(
                oldPath,
                "8/25/2026 08:10:32.0262  SPELL_AURA_APPLIED,Player-509-05585900,\"OtherPlayer\",0x548,0x80000000,Player-510-00000001,\"Old-Vol'jin-EU\",0x511,0x80000000,21562,\"Power Word: Fortitude\",0x2,BUFF\n");

            await File.WriteAllTextAsync(
                latestPath,
                "8/26/2026 17:00:00.0000  SPELL_AURA_APPLIED,Player-509-05585900,\"OtherPlayer\",0x548,0x80000000,Player-510-00000002,\"Azuriel-Vol'jin-EU\",0x511,0x80000000,21562,\"Power Word: Fortitude\",0x2,BUFF\n" +
                "8/26/2026 17:01:00.0000  CHALLENGE_MODE_START,\"Antre de Nalorakk\",2825,999,9,[1,2,3,4]\n" +
                "8/26/2026 17:02:00.0000  SPELL_AURA_APPLIED,Player-509-05585900,\"OtherPlayer\",0x548,0x80000000,Player-510-00000003,\"Shaigan-Vol'jin-EU\",0x511,0x80000000,21562,\"Power Word: Fortitude\",0x2,BUFF\n");

            File.SetLastWriteTimeUtc(oldPath, DateTime.UtcNow.AddMinutes(-1));
            File.SetLastWriteTimeUtc(latestPath, DateTime.UtcNow);

            var reader = new CombatLogReader(logsPath);
            var player = await reader.FindLastPlayerObservedAsync();

            Assert.NotNull(player);
            Assert.Equal("Player-510-00000003", player.Guid);
            Assert.Equal("Shaigan-Vol'jin-EU", player.Name);
        }
        finally
        {
            Directory.Delete(logsPath, recursive: true);
        }
    }

    [Fact]
    public async Task FindLastPlayerObservedReturnsNullWithoutLogs()
    {
        var logsPath = Path.Combine(Path.GetTempPath(), $"wow-stream-overlay-logs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(logsPath);

        try
        {
            var reader = new CombatLogReader(logsPath);

            Assert.Null(await reader.FindLastPlayerObservedAsync());
        }
        finally
        {
            Directory.Delete(logsPath, recursive: true);
        }
    }
}
