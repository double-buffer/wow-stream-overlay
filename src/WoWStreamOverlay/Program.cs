using Microsoft.Extensions.Configuration;
using WowStreamOverlay;
using WowStreamOverlay.CombatLog;

Console.WriteLine("WoW Stream Overlay v0.1.0 · https://github.com/double-buffer/wow-stream-overlay");

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets(typeof(BattleNetClient).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var charactersPath = configuration["Cache:CharactersPath"];

if (string.IsNullOrWhiteSpace(charactersPath))
{
    charactersPath = "characters.json";
}

var characterCache = new CharacterCache(charactersPath);

await characterCache.LoadAsync();

var cachedCharacter = characterCache.Get("Player-510-001577CC");

if (cachedCharacter is not null)
{
    Console.WriteLine(
        $"Cached Character: {cachedCharacter.Profile.Name}, " +
        $"Spec: {cachedCharacter.Profile.Specialization}, " +
        $"iLvl: {cachedCharacter.Profile.ItemLevel}");
}

var clientId = configuration["BattleNet:ClientId"];
var clientSecret = configuration["BattleNet:ClientSecret"];
var region = configuration["BattleNet:Region"];
var locale = configuration["BattleNet:Locale"];

if (string.IsNullOrWhiteSpace(clientId))
{
    Console.Error.WriteLine("Error: BattleNet:ClientId is not configured.");
    return;
}

if (string.IsNullOrWhiteSpace(clientSecret))
{
    Console.Error.WriteLine("Error: BattleNet:ClientSecret is not configured.");
    return;
}

if (string.IsNullOrWhiteSpace(region))
{
    region = "eu";
}

if (string.IsNullOrWhiteSpace(locale))
{
    locale = "fr_FR";
}

var battleNetConfigured =
    !string.IsNullOrWhiteSpace(clientId) &&
    !string.IsNullOrWhiteSpace(clientSecret);

if (!battleNetConfigured)
{
    Console.WriteLine("Battle.net integration is not configured.");
}
else
{
    using var httpClient = new HttpClient();
    var client = new BattleNetClient(httpClient, clientId, clientSecret, region, locale);

    var character = await client.GetCharacterProfileAsync("voljin", "shaigan");

    if (character is not null)
    {
        Console.WriteLine($"Found Character: {character.Name}, Spec: {character.Specialization}, iLvl: {character.ItemLevel}");

        if (character is not null)
        {
            const string guid = "Player-510-001577CC";

            characterCache.Set(
                guid,
                character,
                CharacterRefreshSource.BattleNet);

            await characterCache.SaveAsync();
        }
    }
}

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

