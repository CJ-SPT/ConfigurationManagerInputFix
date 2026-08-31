# Configuration Manager Input Fix

A standalone BepInEx 5 compatibility shim for SPT/Escape from Tarkov.

## Cause and scope

ConfigurationManager v18.4 caches the values returned by
`UnityInput.Current.SupportedKeyCodes` in the private static
`ConfigurationManager.SettingFieldDrawer._keysToCheck` field. Its native filter excludes
`None` and `Mouse0`, but not `F13`. Some overlays and controller software emit a synthetic
F13 release, so Configuration Manager can capture and save F13 while waiting for a keybind.

This plugin uses an SPT `ModulePatch` prefix on Configuration Manager's setting drawer. When
keybind capture is active, the prefix filters `None`, `Mouse0`, and `F13` from the private
cached capture list immediately before Configuration Manager scans it. It does not patch
Unity input globally and it does not inspect or rewrite existing configuration values.

The `Enabled` setting defaults to `true`. There is no per-frame `Update` polling. When disabled
or unloaded after filtering a capture, the plugin sets the private cache field to `null`,
allowing Configuration Manager to reconstruct its native list.

## Build

The project targets `netstandard2.1`. Its BepInEx and Unity references use paths relative to
the nested project and expect the solution directory to remain at:

`<SPT root>/Development/ConfigurationManagerInputFix`

From this directory, run:

```powershell
dotnet build .\ConfigurationManagerInputFix.sln -c Release
```

## Output and install

The plugin DLL is produced at:

`ConfigurationManagerInputFix/bin/Release/netstandard2.1/ConfigurationManagerInputFix.dll`

To install it, copy that DLL into a folder under `<SPT root>/BepInEx/plugins`. Do not copy the
locally referenced BepInEx or Unity assemblies. Configuration Manager must be installed; it
is declared as a hard dependency. The plugin is scoped to `EscapeFromTarkov.exe`.

## Limitations

- The reflection target is the private v18.4 implementation and may need adjustment if
  Configuration Manager renames or changes `SettingFieldDrawer._keysToCheck`.
- F13 remains valid everywhere else in Unity and in existing config values; only new keybind
  capture through Configuration Manager is filtered.
- Other synthetic keys are not filtered.
