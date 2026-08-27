namespace WowStreamOverlay;

public interface ICharacterProfileProvider
{
    CharacterRefreshSource RefreshSource { get; }

    Task<CharacterProfile?> GetCharacterProfileAsync(
        string realmSlug,
        string characterName,
        CancellationToken cancellationToken = default);
}
