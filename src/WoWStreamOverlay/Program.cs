using WowStreamOverlay.CombatLog;

Console.WriteLine("WoW Stream Overlay v0.1.0 · https://github.com/double-buffer/wow-stream-overlay");

var parser = new CombatLogParser();
using var streamReader = new StreamReader("TestLog.txt");

var line = streamReader.ReadLine();

while (line is not null)
{
    var result = parser.ParseLine(line);

    if (result.Status == ParseStatus.Parsed)
    {
        if (result.Event is ChallengeModeStartedEvent)
        {
            var eventValue = result.Event as ChallengeModeStartedEvent;
            Console.WriteLine($"MythicPlus Started: {eventValue!.DungeonName}, +{eventValue!.Level}");
        }
        
        else if (result.Event is ChallengeModeEndedEvent)
        {
            var eventValue = result.Event as ChallengeModeEndedEvent;
            Console.WriteLine($"MythicPlus Ended Completed: {eventValue!.Completed}");
        }
    }

    line = streamReader.ReadLine();
}

