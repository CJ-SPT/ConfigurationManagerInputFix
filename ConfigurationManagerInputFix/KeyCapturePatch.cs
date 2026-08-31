using System.Reflection;
using BepInEx;
using SPT.Reflection.Patching;
using UnityEngine;

namespace ConfigurationManagerInputFix;

internal sealed class KeyCapturePatch : ModulePatch
{
    private const string ConfigurationManagerAssemblyName = "ConfigurationManager";
    private const string SettingFieldDrawerTypeName = "ConfigurationManager.SettingFieldDrawer";
    private const string DrawSettingValueMethodName = "DrawSettingValue";
    private const string KeysToCheckFieldName = "_keysToCheck";
    private const string CurrentShortcutFieldName = "_currentKeyboardShortcutToSet";

    private static FieldInfo? _keysToCheckField;
    private static FieldInfo? _currentShortcutField;
    private static bool _ownsCaptureList;
    private static bool _accessFailureLogged;

    protected override MethodBase GetTargetMethod()
    {
        var drawerType = ResolveDrawerType();
        _keysToCheckField = ResolveField(drawerType, KeysToCheckFieldName);
        _currentShortcutField = ResolveField(drawerType, CurrentShortcutFieldName);

        if (!_keysToCheckField.FieldType.IsAssignableFrom(typeof(KeyCode[])))
        {
            throw new InvalidOperationException(
                $"{SettingFieldDrawerTypeName}.{KeysToCheckFieldName} has an incompatible type: " +
                _keysToCheckField.FieldType.FullName);
        }

        return drawerType.GetMethod(DrawSettingValueMethodName,BindingFlags.Public | BindingFlags.Instance)
               ?? throw new MissingMethodException(SettingFieldDrawerTypeName, DrawSettingValueMethodName);
    }

    [PatchPrefix]
    private static void PatchPrefix()
    {
        if (!Plugin.FilterEnabled || _ownsCaptureList || _currentShortcutField?.GetValue(null) is null)
        {
            return;
        }

        try
        {
            var currentKeys = _keysToCheckField?.GetValue(null) as IEnumerable<KeyCode>;
            if (currentKeys is not null && !currentKeys.Any(KeyCaptureFilter.IsBlocked))
            {
                return;
            }

            var supportedKeys = currentKeys ?? UnityInput.Current?.SupportedKeyCodes;
            if (supportedKeys is null)
            {
                LogAccessFailure("BepInEx input did not provide a supported-key list", null);
                return;
            }

            var filteredKeys = KeyCaptureFilter.Filter(supportedKeys);
            _keysToCheckField!.SetValue(null, filteredKeys);
            _ownsCaptureList = true;
            _accessFailureLogged = false;
        }
        catch (Exception exception)
        {
            LogAccessFailure("Could not filter Configuration Manager's key capture list", exception);
        }
    }

    internal static void RestoreNativeCaptureList(string reason)
    {
        if (!_ownsCaptureList || _keysToCheckField is null)
        {
            return;
        }

        try
        {
            _keysToCheckField.SetValue(null, null);
            _ownsCaptureList = false;
            _accessFailureLogged = false;
            Plugin.Log.LogInfo(
                $"Cleared Configuration Manager key capture cache ({reason}); " +
                "its native list will be reconstructed on demand.");
        }
        catch (Exception exception)
        {
            LogAccessFailure("Could not clear Configuration Manager's key capture cache", exception);
        }
    }

    private static Type ResolveDrawerType()
    {
        var configurationManagerAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly =>
                string.Equals(
                    assembly.GetName().Name,
                    ConfigurationManagerAssemblyName,
                    StringComparison.Ordinal));

        if (configurationManagerAssembly is null)
        {
            throw new InvalidOperationException(
                "ConfigurationManager assembly was not loaded despite the declared hard dependency.");
        }

        return configurationManagerAssembly.GetType(SettingFieldDrawerTypeName, false)
               ?? throw new TypeLoadException(
                   $"Could not find {SettingFieldDrawerTypeName}; this Configuration Manager version is incompatible.");
    }

    private static FieldInfo ResolveField(Type drawerType, string fieldName) =>
        drawerType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(drawerType.FullName, fieldName);

    private static void LogAccessFailure(string message, Exception? exception)
    {
        if (_accessFailureLogged)
        {
            return;
        }

        var detail = exception is null
            ? string.Empty
            : $" {exception.GetType().Name}: {exception.Message}";
        
        Plugin.Log.LogWarning($"{message}; global input was not changed.{detail}");
        _accessFailureLogged = true;
    }
}
