# SeedMaskProbe

[![EXILED](https://img.shields.io/badge/EXILED-9.14.2-5865F2)](https://github.com/ExMod-Team/EXILED) [![LabAPI](https://img.shields.io/badge/LabAPI-1.1.7-5865F2)](https://github.com/northwood-studios/LabAPI) [![SCP:SL](https://img.shields.io/badge/SCP%3ASL-14.2.7-2f3136)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![.NET%20Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)](https://dotnet.microsoft.com/download/dotnet-framework) [![Release](https://img.shields.io/github/v/release/qingranawa/SeedMaskProbe?display_name=tag&sort=semver)](https://github.com/qingranawa/SeedMaskProbe/releases/latest) [![CI](https://github.com/qingranawa/SeedMaskProbe/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/qingranawa/SeedMaskProbe/actions/workflows/ci.yml) [![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

SeedMaskProbe is an EXILED server-side plugin for SCP: Secret Laboratory that hides the map Seed shown to ordinary players through the client console `seed` command.

---

## Features

- Sends an accepted fake Seed to each client after map generation.
- Prevents players from obtaining the real Seed for the current round through the Seed command.
- Supports either a fixed fake Seed value or a fake value generated randomly each round.

## Important Limitation

> The client must still receive the real Seed in order to generate the map correctly.

## Compatibility

- SCP: Secret Laboratory: `14.2.7`.
- EXILED: `9.14.2`.
- Target framework: `.NET Framework 4.8`.

## Installation

1. Download `SeedMaskProbe.dll` from Releases.

2. Place the DLL at:

```text
AppData/EXILED/Plugins/SeedMaskProbe.dll
```

3. The plugin automatically creates the following file on its first start:

```text
AppData/EXILED/Plugins/SeedMaskProbe/seed-mask.json
```

---

## Fake Seed Configuration

`seed-mask.json` contains a single configuration field: `fake_seed`.

**Fixed value:**

```json
{
  "fake_seed": "246813579"
}
```

**Random value generated each round:**

```json
{
  "fake_seed": "${RANDOM}"
}
```

Fixed values should preferably be quoted positive decimal integers in the following range:

```text
1 to 2147483646
```

For a value that looks like a 10-digit native Seed, use the following range:

```text
1000000000 to 2147483646
```

If the field is missing, empty, or invalid, the plugin falls back to its built-in fake value. If the configured value matches the current real Seed, the plugin automatically adjusts it to a neighboring valid value.

## Delay Configuration

Recommended baseline configuration for testing and production servers:

```yaml
is_enabled: true
debug: false
mask_delay_seconds: 0.1
late_join_mask_delay_seconds: 0.1
```

Both values control the server-side timer delay for the client-connection or map-generation masking paths. The current code enforces a minimum delay of `0.1` seconds.

`0.1` seconds reduces the chance that an ordinary player catches the real value during authentication, but it cannot guarantee that the real Seed never reaches the client. `debug` is disabled by default; when enabled, it logs the fake value and the connected player's identifier. Keep it set to `false` in production.

## Build

Building requires the .NET SDK, the .NET Framework 4.8 Developer Pack, and the Managed assemblies from the target SCP:SL version. The game assemblies are not included in this repository or in the published package.

Pass the Managed directory explicitly in PowerShell:

```powershell
dotnet build .\SeedMaskProbe.csproj -c Release `
  -p:ScpSlManagedPath="C:\Path\To\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed"
```

You can also use the environment variable:

```powershell
$env:SCPSL_MANAGED_PATH = "C:\Path\To\SCPSL_Data\Managed"
dotnet build .\SeedMaskProbe.csproj -c Release
```

If `ScpSlManagedPath` or the required assemblies are missing, the project fails explicitly instead of using a developer-specific default path.

## Tests

**Pure logic tests:**

```powershell
dotnet run --project .\tests\SeedMaskProbe.Tests\SeedMaskProbe.Tests.csproj -c Release
```

The tests cover fixed values, `${RANDOM}`, invalid-value fallback, range normalization, and real-Seed collision correction.

The final release file is located at:

```text
release/SeedMaskProbe.dll
```

## License

This project is licensed under the MIT License.
