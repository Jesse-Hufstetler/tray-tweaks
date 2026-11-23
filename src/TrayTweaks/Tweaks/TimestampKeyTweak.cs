using System.Globalization;
using TrayTweaks.Input;

namespace TrayTweaks.Tweaks;

/// <summary>
/// Win + Right arrow types the current date and time in a form that sorts and is safe in a file
/// name. It takes the key away from Windows' own Win + Right (snap the window right). Windows won't
/// let that key be registered as a hot key, hence the hook.
/// </summary>
public sealed class TimestampKeyTweak : ToggleTweak
{
    WinKeyHook? _hook;

    public override string Id => "timestamp-key";
    public override string Name => "Timestamp key";
    public override string Description => "Win + Right arrow types the date and time, like 2026_11_23__14_05_09.";
    public override string Glyph => "";

    public override void Enable() => _hook ??= new WinKeyHook(Keyboard.Right, TypeTimestamp);

    public override void Disable()
    {
        _hook?.Dispose();
        _hook = null;
    }

    public static string Format(DateTime time) => time.ToString("yyyy_MM_dd__HH_mm_ss", CultureInfo.InvariantCulture);

    static void TypeTimestamp()
    {
        // The Windows key is still held when this runs.
        Keyboard.ReleaseWin();
        Task.Run(() => Keyboard.TypeText(Format(DateTime.Now)));
    }
}
