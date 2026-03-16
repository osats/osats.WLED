# osats.WLED — SimHub WLED Plugin

Fork of [Halcyon WLED](https://github.com/HalcyonAlcedo/SimHub-WLED) with extended telemetry effects.

## Features

- 🔴 Default (SimHub open, stopped, menus) — solid red
- 🔵 Driving — solid blue
- 🟢 Green flag — fast blink for 5 seconds
- 🟡 Yellow flag — slow blink
- 🔵 Blue flag — slow blink  
- 🟠 Pit limiter — fast orange blink
- 🎨 Configurable colours per effect with live preview
- ✅ Enable/disable each effect individually

## Requirements

- [SimHub](https://www.simhubdash.com/)
- WLED device on the same network
- .NET Framework 4.8

## Installation

1. Download `osats.WLED.dll` from Releases
2. Copy to `C:\Program Files (x86)\SimHub\`
3. Restart SimHub
4. Go to **WLED** in the left menu and configure your device IP

## Building

Open `Osat.WLED.csproj` in Visual Studio and press `Ctrl+Shift+B`.

## Credits

Based on [Halcyon WLED](https://github.com/HalcyonAlcedo/SimHub-WLED) by Halcyon.

## License

MIT