using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ConfigurationManagerInputFix;

[BepInPlugin("com.cj.configurationmanagerinputfix", "Configuration Manager Input Fix", "1.0.0")]
[BepInDependency("com.bepis.bepinex.configurationmanager")]
public sealed class Plugin : BaseUnityPlugin
{
    private ConfigEntry<bool>? _enabled;
    private Harmony? _harmony;

    internal static ManualLogSource Log { get; private set; } = null!;
    internal static bool FilterEnabled => Instance?._enabled?.Value == true;
    private static Plugin? Instance { get; set; }

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        _enabled = Config.Bind(
            "General",
            "Enabled",
            true,
            "Exclude virtual F13 input from Configuration Manager keybind capture, such as from overlays, controllers, or any other virtual key provider");
        
        _enabled.SettingChanged += EnabledOnSettingChanged;

        try
        {
            _harmony = new Harmony("com.cj.configurationmanagerinputfix");
            KeyCapturePatch.Enable(_harmony);
        }
        catch (Exception exception)
        {
            Logger.LogError(
                $"Could not patch Configuration Manager key capture; the fix is inactive. " +
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private void OnDestroy()
    {
        if (_enabled is not null)
        {
            _enabled.SettingChanged -= EnabledOnSettingChanged;
        }

        KeyCapturePatch.RestoreNativeCaptureList("plugin destroyed");

        try
        {
            _harmony?.UnpatchSelf();
        }
        catch (Exception exception)
        {
            Logger.LogWarning($"Could not disable the key capture patch cleanly: {exception.Message}");
        }

        Instance = null;
    }

    private static void EnabledOnSettingChanged(object sender, EventArgs eventArgs)
    {
        if (!FilterEnabled)
        {
            KeyCapturePatch.RestoreNativeCaptureList("disabled in config");
        }
    }
}
