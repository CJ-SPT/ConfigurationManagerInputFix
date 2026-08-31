using UnityEngine;

namespace ConfigurationManagerInputFix;

internal static class KeyCaptureFilter
{
    internal static KeyCode[] Filter(IEnumerable<KeyCode> supportedKeyCodes)
    {
        if (supportedKeyCodes is null)
            throw new ArgumentNullException(nameof(supportedKeyCodes));

        return supportedKeyCodes.Where(IsCapturable).ToArray();
    }

    private static bool IsCapturable(KeyCode keyCode) =>
        !IsBlocked(keyCode);

    internal static bool IsBlocked(KeyCode keyCode) =>
        keyCode is KeyCode.None or KeyCode.Mouse0 or KeyCode.F13;
}
