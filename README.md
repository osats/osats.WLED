# osats.WLED — SimHub lighting studio

Configurable telemetry lighting for WLED, based on [Halcyon WLED](https://github.com/HalcyonAlcedo/SimHub-WLED).

## Installation

1. Close SimHub. Back up your existing osats.WLED.dll and WLED settings in PluginsData.
2. Extract osats.WLED.dll from the distribution ZIP into the SimHub installation folder (normally C:\Program Files (x86)\SimHub).
3. Start SimHub, enable the plugin and open **WLED** in the left menu.
4. Enter your device's IPv4 address, UDP port, LED count and first LED index. Replace the placeholder address with your WLED address.
5. Enable realtime UDP reception in WLED on the same port. This plugin defaults to **19446**; the device must match.
6. Apply and save, then test an effect. Tests end automatically.

Requires Windows, SimHub, .NET Framework 4.8 and a reachable WLED device. UDP has no acknowledgement: “UDP enviado” does not confirm hardware reception.

## Configuration

- Colour picker or #RRGGBB input with local visual preview.
- Global brightness multiplied by per-effect brightness.
- Enable/disable, priority, blinking and blink interval per effect.
- RPM start/end percentage; forward, reverse, centre out and edges in directions.
- Configurable green-flag and test durations.
- Tests work without a game and **apply/save all panel settings** before starting.
- Higher priority wins; ties resolve by effect key. Menus/no-game use the default effect exclusively.
- If no enabled effect applies, the configured zone is black.

| Effect | Default priority | Behaviour |
|---|---:|---|
| Menus / no game | 0 | Solid red |
| Driving fallback | 0 | Solid blue |
| RPM | 10 | Blue bar, 50–100% of maximum RPM |
| Shift alert | 20 | Red blink |
| Green flag | 30 | Green blink for five seconds |
| Blue flag | 40 | Blue blink |
| Yellow flag | 50 | Yellow blink |
| Pit limiter | 60 | Orange blink |

Signals depend on the game. Stationary cars can show flags, RPM and limiter. RPM takes precedence over the driving fallback when maximum RPM is valid. Shift detection uses SimHub's first shift-light signal.

## Share profiles

Export/import .wledprofile XML files. Export replaces your IP/port with generic defaults. Import preserves the recipient's current connection, count, offset and direction. Review and apply imported settings explicitly. Unsupported newer profile versions are rejected.

Existing effect colours and enabled flags migrate automatically. Version 2 introduces global brightness (60%), a 50–100% RPM range and a blinking shift alert. Keep a backup: new effect options are not migrated back to version 1. Invalid startup settings are logged and replaced in memory with defaults.

## Limits

- Indexed UDP supports LED indices **0–255**: count plus offset must not exceed 256.
- A single winning effect controls the configured zone. Multiple zones, radar and automatic game/car profiles are future work.
- The colour preview is local; use a timed test to preview on the strip.
- Unchanged frames refresh every 500 ms to maintain the one-second realtime timeout. Frame rate is configurable from 1–60 FPS.
- Closing SimHub stops transmission; subsequent output follows the device's realtime timeout settings.
- User confirmed the plugin is working on 2026-10-26.

## Build and package

Install Visual Studio Build Tools with .NET desktop build tools and the .NET Framework 4.8 targeting pack. SimHub supplies SDK assemblies; these are not redistributed.

Run from PowerShell:

    .\tools\Build.ps1
    # For another SimHub path:
    .\tools\Build.ps1 -SimHubDir 'D:\SimHub'

The script builds Release, runs regression tests and produces artifacts\osats.WLED-2.0.0.zip. Builds stay in the repository and never overwrite an installed plugin.

Optional WPF render smoke test:

    powershell -STA -File .\tools\Preview.ps1

## Credits and licence

MIT. Original copyright and licence retained in [LICENSE](LICENSE).
Original Halcyon WLED by Halcyon; osats fork and telemetry/configuration enhancements.
