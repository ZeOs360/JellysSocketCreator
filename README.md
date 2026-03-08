# Jelly's Socket Creator

<p align="center">
  <img src="jelly.png" alt="Jelly's Socket Creator" width="120">
</p>

<p align="center">
  <strong>Add custom CPU sockets to PC Building Simulator 2</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#installation">Installation</a> •
  <a href="#usage">Usage</a> •
  <a href="#building">Building</a> •
  <a href="#license">License</a>
</p>

---

## Features

- ✅ **Custom Socket Names** - Add your own CPU socket types (AM6, AM7, LGA 1851, etc.)
- ✅ **Full Compatibility** - Custom sockets work with CPU/Motherboard matching
- ✅ **JSON Config** - Simple configuration file format
- ✅ **One-Click Deploy** - Deploy to game folder instantly

## Requirements

- PC Building Simulator 2 (Epic Games version)
- Windows 10/11 x64
- .NET 9.0 Runtime (for Config UI)

## Installation

### Easy Installation (Recommended)

1. Download the latest release from [Nexus Mods](https://www.nexusmods.com/pcbuildingsimulator2/mods/) or [Releases](https://github.com/ZeOs360/JellysSocketCreator/releases)
2. Run `JellysSocketsConfig.exe`
3. Set your game installation path
4. Add your custom sockets
5. Click **Deploy**
6. Launch the game!

### Manual Installation

1. Copy `version.dll` to your game folder
2. Copy `JellysSockets.json` to your game folder
3. Edit `JellysSockets.json` with your sockets

## Usage

### Configuration File Format

```json
{
    "sockets": [
        {"id": 100, "name": "AM6"},
        {"id": 101, "name": "AM7"},
        {"id": 102, "name": "LGA 1851"}
    ]
}
```

### Creating Custom Parts

In your mod's XML files, reference the socket ID or name:

```xml
<!-- Custom CPU -->
<CPU>
    <Name>AMD Ryzen 9 9950X</Name>
    <CPU Socket value="100"/>
</CPU>

<!-- Custom Motherboard -->
<Motherboard>
    <Name>ASUS ROG CROSSHAIR X870E</Name>
    <CPU Socket value="100"/>
</Motherboard>
```

> **Note:** Socket IDs must be between 100-999 to avoid conflicts with vanilla sockets.

## Building from Source

### Prerequisites

- Visual Studio 2022 with C++ Desktop Development workload
- .NET 9.0 SDK
- MinHook library (included)

### Build Steps

```bash
# Clone the repository
git clone https://github.com/ZeOs360/JellysSocketCreator.git
cd JellysSocketCreator

# Build the DLL
cd JellysSockets
msbuild JellysSockets.vcxproj /p:Configuration=Release /p:Platform=x64

# Build the Config UI
cd ../JellysSocketsConfig
dotnet build -c Release
```

## Project Structure

```
JellysSockets/
├── JellysSockets/              # C++ DLL Project
│   ├── dllmain.cpp             # Main injection code
│   ├── JellysSockets.vcxproj   # VS Project file
│   └── ...
├── JellysSocketsConfig/        # C# Config UI
│   ├── Form1.cs                # Main UI form
│   ├── JellysSocketsConfig.csproj
│   └── ...
├── Release/                    # Distribution files
│   ├── version.dll
│   ├── JellysSocketsConfig.exe
│   ├── JellysSockets.json
│   └── README.md
└── README.md
```

## How It Works

JellysSockets uses DLL proxy injection to hook into the game's IL2CPP runtime:

1. **version.dll Proxy** - The game loads our DLL thinking it's the Windows version.dll
2. **MinHook** - Hooks into game functions to intercept socket-related calls
3. **s_names/s_used Patch** - Extends the socket name arrays at runtime
4. **ImportProp Hook** - Intercepts XML parsing to handle custom socket values
5. **IsCompatible Hook** - Enables compatibility for custom socket matches
6. **GetUIName Hook** - Returns custom socket names for UI display

## Known Limitations

- Custom sockets won't appear in the shop filter dropdown (IL2CPP AOT limitation)
- Requires game restart after config changes

## Troubleshooting

Check `JellysSockets.log` in your game folder for diagnostic information.

Common issues:
- **DLL not loading**: Make sure `version.dll` is in the game's root folder
- **Sockets not appearing**: Check `JellysSockets.json` syntax is valid
- **Game crash**: Check log file for errors

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

- [MinHook](https://github.com/TsudaKageyu/minhook) library by TsudaKageyu
---

<p align="center">
  Made with ❤️ for the PCBS2 modding community
</p>
