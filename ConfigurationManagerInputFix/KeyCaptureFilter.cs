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

    internal static bool IsCapturable(KeyCode keyCode) =>
        !IsBlocked(keyCode);

    internal static bool IsBlocked(KeyCode keyCode) =>
        keyCode == KeyCode.None ||
        keyCode == KeyCode.Mouse0 ||
        keyCode == KeyCode.F13;
}
