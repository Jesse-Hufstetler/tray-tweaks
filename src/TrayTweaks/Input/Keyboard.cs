using System.Runtime.InteropServices;

namespace TrayTweaks.Input;

/// <summary>Sends key presses to whichever window has the focus.</summary>
public static class Keyboard
{
    public const ushort Enter = 0x0D;
    public const ushort Control = 0x11;
    public const ushort Alt = 0x12;
    public const ushort F4 = 0x73;

    public static ushort Letter(char letter) => char.ToUpperInvariant(letter);

    /// <summary>Presses the keys in order and releases them in reverse, e.g. Chord(Control, Letter('w')).</summary>
    public static void Chord(params ushort[] keys)
    {
        var inputs = new INPUT[keys.Length * 2];
        for (var i = 0; i < keys.Length; i++)
        {
            inputs[i] = Key(keys[i], up: false);
            inputs[inputs.Length - 1 - i] = Key(keys[i], up: true);
        }
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    static INPUT Key(ushort key, bool up) => new()
    {
        type = INPUT_KEYBOARD,
        u = new() { ki = new() { wVk = key, dwFlags = up ? KEYEVENTF_KEYUP : 0 } },
    };

    const uint INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    // MOUSEINPUT is the union's largest member; it is here only so INPUT gets the size Windows expects.
    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);
}
