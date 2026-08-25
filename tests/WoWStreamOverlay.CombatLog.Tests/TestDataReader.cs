namespace WowStreamOverlay.CombatLog.Tests;

internal static class TestDataReader
{
    public static string ReadLine(string path)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", path)).TrimEnd('\r', '\n');
    }
}
