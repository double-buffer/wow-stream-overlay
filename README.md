# WoW Stream Overlay

A lightweight local overlay server for World of Warcraft streams.

WoW Stream Overlay reads the World of Warcraft combat log, keeps a small local game state, and exposes HTML overlays that can be used directly as OBS Browser Sources.

The project is intentionally local and simple: no permanent overlay is drawn over the game itself, no external web service is required for rendering, and the HTML remains fully customizable.

## How it works

```text
WoWCombatLog.txt
      ↓
Combat log parser
      ↓
GameState
      ↓
Kestrel HTTP + Server-Sent Events
      ↓
HTML / CSS overlay
      ↓
OBS Browser Source
```

The application currently tracks the active character and Mythic+ state, persists the latest known character information, and pushes live state changes to connected overlays through Server-Sent Events.

## Requirements

- Windows 10 or Windows 11
- World of Warcraft Retail
- OBS Studio or another browser-source compatible application
- Combat logging enabled in World of Warcraft

Release packages are self-contained and do not require a separate .NET installation. The Windows executable is published as a trimmed, compressed single-file application to keep the distribution as small as possible while retaining the self-contained runtime.

## Installation

1. Download the latest Windows x64 zip from the GitHub Releases page.
2. Extract it to a directory of your choice.
3. Edit `appsettings.json` and set `Wow:LogsPath` to your Retail combat log directory.

Example:

```json
{
  "Wow": {
    "LogsPath": "C:\\Program Files (x86)\\World of Warcraft\\_retail_\\Logs"
  }
}
```

4. Install the bundled WoW addon:

```text
WowStreamOverlay addon install
```

5. Start the application:

```text
WowStreamOverlay
```

6. Add the overlay to OBS as a Browser Source.

The default header overlay is available at:

```text
http://127.0.0.1:37231/overlay/header
```

## WoW addon

The bundled addon is deliberately tiny. It helps initialize combat logging when entering the game so the desktop application can discover the current character through the combat log.

On first run, when no persisted character is available yet, the application scans the latest combat log for the most recently observed local player before switching to normal live following at the end of the file. Historical Mythic+ state is not replayed during this bootstrap.

The addon is versioned independently from the desktop application. Its version is stored directly in:

```text
src/Addon/WoWStreamOverlay/WoWStreamOverlay.toc
```

Useful commands:

```text
WowStreamOverlay addon install
WowStreamOverlay addon update
WowStreamOverlay addon uninstall
```

`addon update` compares the installed addon version with the bundled addon version and never intentionally downgrades a newer installed version.

## Configuration

The default configuration is stored in `appsettings.json`.

```json
{
  "Wow": {
    "LogsPath": ""
  },
  "BattleNet": {
    "ClientId": "",
    "ClientSecret": "",
    "Region": "eu",
    "Locale": "fr_FR",
    "CharacterRefreshIntervalSeconds": 60
  },
  "Storage": {
    "CharactersPath": "characters.json",
    "StatePath": "state.json"
  },
  "Web": {
    "Host": "127.0.0.1",
    "Port": 37231
  },
  "Overlays": {
    "header": {
      "Template": "Overlays/header.html"
    }
  }
}
```

### Battle.net

Battle.net integration uses Blizzard client credentials to refresh character profile information. It does not use player-account OAuth or enumerate characters from an account.

If `BattleNet:ClientId` and `BattleNet:ClientSecret` are left empty, the application still runs but Battle.net profile refresh is disabled.

### Overlays

Each configured overlay maps a URL name to an HTML template:

```json
"Overlays": {
  "header": {
    "Template": "Overlays/header.html"
  }
}
```

This becomes:

```text
http://127.0.0.1:37231/overlay/header
```

Templates can use the runtime `data-field`, `data-visible-field`, and `data-color-field` attributes. The application injects the small client runtime used to receive live state updates through Server-Sent Events.

## Command line

```text
WowStreamOverlay                  Run the application
WowStreamOverlay status           Show configuration and runtime status
WowStreamOverlay addon install    Install the bundled WoW addon
WowStreamOverlay addon update     Update the installed WoW addon
WowStreamOverlay addon uninstall  Uninstall the bundled WoW addon
WowStreamOverlay --version        Show the exact application build version
WowStreamOverlay help             Show command help
```

`status` reports the configured WoW logs path, addon state and versions, Battle.net configuration state, web endpoint, overlay URLs, and local storage paths.

The exact build version is intentionally visible in the application banner, `status`, and `--version` output so screenshots and logs can be tied back to a specific released build.

## Versioning

The desktop application and WoW addon are versioned independently.

Application versions follow this project lifecycle:

```text
1.0.0-dev.1
1.0.0-dev.2
1.0.0-alpha.3
1.0.0-ptr.4
1.0.0-rc.5
1.0.0
```

`ptr` is simply this project's WoW-flavored name for the public test stage. It is not related to Blizzard's PTR or to the World of Warcraft game version.

The product version and release stage are defined in `Directory.Build.props`. Local builds use `local` as their build identifier. The current 1.0 line is in alpha:

```text
1.0.0-alpha.local
```

Official builds receive a global build number from the release workflow:

```text
1.0.0-alpha.3
```

Stable releases have no stage suffix:

```text
1.0.0
```

## Releases

Every commit pushed to `main` is automatically built and published as a GitHub Release.

The release workflow:

1. restores, builds, and tests the solution;
2. publishes a self-contained, trimmed and compressed Windows x64 build;
3. reads the exact version back from `WowStreamOverlay.exe`;
4. packages the output as `WowStreamOverlay-v<version>-win-x64.zip`;
5. creates the matching Git tag `v<version>` on the exact `main` commit;
6. generates release notes from the pull requests associated with the release range;
7. creates the GitHub Release and uploads the package.

`dev`, `alpha`, `ptr`, and `rc` builds are marked as GitHub pre-releases. Stable versions such as `1.0.0` are normal releases.

Release notes are stage-aware. Consecutive releases within the same prerelease stage describe only the changes since the previous release. The first release after a stage transition (`dev` → `alpha`, `alpha` → `ptr`, or `ptr` → `rc`) contains the full current product-version cycle since the previous stable release, or since the beginning of the project when no stable release exists. Stable releases are also cumulative since the previous stable release. This gives existing testers concise incremental notes while giving testers entering at a new maturity level a complete view of the release.

Commits without an associated pull request are listed separately so changes are not silently lost. For associated pull requests, both the PR title and PR description are included in the release notes.

## Development

The project targets .NET 10.

```text
dotnet restore WoWStreamOverlay.slnx
dotnet build WoWStreamOverlay.slnx
dotnet test WoWStreamOverlay.slnx
```

Run from source with:

```text
dotnet run --project src/WoWStreamOverlay
```

The source tree is intentionally small:

```text
src/
├── Addon/
├── WoWStreamOverlay/
└── WoWStreamOverlay.CombatLog/

tests/
├── WoWStreamOverlay.CombatLog.Tests/
└── WoWStreamOverlay.Tests/
```

## Known limitations

- The project currently targets World of Warcraft Retail.
- Combat log writes are buffered by World of Warcraft. In quiet open-world situations, a newly written event can take some time to appear on disk.
- Active Mythic+ state is intentionally transient and is reset when the application restarts.
- The default web server binds only to `127.0.0.1`.

## License

MIT. See [LICENSE](LICENSE).
