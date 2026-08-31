## Problem

ConfigurationManager caches the values returned by
`UnityInput.Current.SupportedKeyCodes` in the private static
`ConfigurationManager.SettingFieldDrawer._keysToCheck` field. Its native filter excludes
`None` and `Mouse0`, but not `F13`. Some overlays and controller software emit a virtual
F13 release, so Configuration Manager can capture and save F13 while waiting for a keybind. This plugin fixes that by not allowing those virtual key presses to go through.

