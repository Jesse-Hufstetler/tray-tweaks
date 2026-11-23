using TrayTweaks.Input;
using TrayTweaks.Secrets;

namespace TrayTweaks.Tweaks;

/// <summary>A key that types the text saved in <paramref name="Slot"/>.</summary>
public sealed record PasswordKey(ushort Key, string KeyName, SecretSlot Slot);

/// <summary>
/// Keys that type a password saved on the Passwords tab. The keys are the F13 to F24 ones the mouse
/// sends; see the G604 table in CLAUDE.md. To add one, add a row to <see cref="Keys"/>.
/// </summary>
public sealed class PasswordKeysTweak : ToggleTweak
{
    public static readonly IReadOnlyList<PasswordKey> Keys =
    [
        new(0x82, "F19", new("f19-password", "G-Shift + G8 (F19)",
            "Password keys tweak: typed when the mouse sends F19.")),
    ];

    readonly List<HotKey> _hotKeys = [];

    public override string Id => "password-keys";
    public override string Name => "Password keys";
    public override string Description => "Mouse buttons that type a password saved on the Passwords tab: " + string.Join(", ", Keys.Select(key => key.Slot.Name)) + ".";
    public override string Glyph => "";

    public override void Enable()
    {
        if (_hotKeys.Count > 0)
            return;
        try
        {
            foreach (var key in Keys)
                _hotKeys.Add(new HotKey(key.Key, key.KeyName, () => Type(key.Slot)));
        }
        catch
        {
            Disable();
            throw;
        }
    }

    public override void Disable()
    {
        _hotKeys.ForEach(hotKey => hotKey.Dispose());
        _hotKeys.Clear();
    }

    static void Type(SecretSlot slot)
    {
        if (SecretStore.Default.Get(slot.Id) is { Length: > 0 } text)
            Task.Run(() => Keyboard.TypeText(text));
    }
}
