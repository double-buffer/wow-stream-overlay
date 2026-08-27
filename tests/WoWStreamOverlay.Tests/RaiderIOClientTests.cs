using System.Net;
using System.Text;
using Xunit;

namespace WowStreamOverlay.Tests;

public class RaiderIOClientTests
{
    [Fact]
    public async Task GetCharacterProfileMapsGearScoreAndLocalizedNames()
    {
        const string json = """
        {
          "name": "Shaigan",
          "race": "Human",
          "class": "Warrior",
          "active_spec_name": "Protection",
          "faction": "alliance",
          "region": "eu",
          "realm": "Vol'jin",
          "mythic_plus_scores_by_season": [
            {
              "scores": {
                "all": 2080.9
              }
            }
          ],
          "gear": {
            "item_level_equipped": 305.312
          }
        }
        """;

        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
        using var httpClient = new HttpClient(handler);
        var client = new RaiderIOClient(httpClient, "eu", "fr_FR");

        var profile = await client.GetCharacterProfileAsync("voljin", "Shaigan");

        Assert.NotNull(profile);
        Assert.Equal(CharacterRefreshSource.RaiderIO, client.RefreshSource);
        Assert.Equal("Shaigan", profile.Name);
        Assert.Equal("Vol'jin", profile.Realm);
        Assert.Equal("voljin", profile.RealmSlug);
        Assert.Equal("eu", profile.Region);
        Assert.Equal(CharacterClass.Warrior, profile.Class);
        Assert.Equal(CharacterSpecialization.ProtectionWarrior, profile.Specialization);
        Assert.Equal(CharacterRace.Human, profile.Race);
        Assert.Equal(305, profile.ItemLevel);
        Assert.Equal(2081, profile.MythicPlusScore);
        Assert.Equal("Guerrier", profile.ClassName);
        Assert.Equal("Protection", profile.SpecializationName);
        Assert.Equal("Humain", profile.RaceName);

        Assert.NotNull(handler.RequestUri);
        Assert.Equal("raider.io", handler.RequestUri.Host);

        var query = Uri.UnescapeDataString(handler.RequestUri.Query);
        Assert.Contains("region=eu", query);
        Assert.Contains("realm=voljin", query);
        Assert.Contains("name=Shaigan", query);
        Assert.Contains("fields=gear,mythic_plus_scores_by_season:current", query);
    }

    [Fact]
    public async Task GetCharacterProfileReturnsNullForNotFound()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.NotFound, "{}");
        using var httpClient = new HttpClient(handler);
        var client = new RaiderIOClient(httpClient);

        var profile = await client.GetCharacterProfileAsync("voljin", "Missing");

        Assert.Null(profile);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public Uri? RequestUri { get; private set; }

        public StubHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            });
        }
    }
}
