using System.Runtime.InteropServices;

namespace TrayTweaks.Input;

/// <summary>Sends key presses to whichever window has the focus.</summary>
public static class Keyboard
{
    public const ushort Tab = 0x09;
    public const ushort Enter = 0x0D;
    public const ushort Shift = 0x10;
    public const ushort Control = 0x11;
    public const ushort Alt = 0x12;
    public const ushort Home = 0x24;
    public const ushort Right = 0x27;
    public const ushort LeftWin = 0x5B;
    public const ushort RightWin = 0x5C;
    public const ushort F4 = 0x73;
    const ushort Unassigned = 0xE8;

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

    /// <summary>Presses and releases the keys one after another, with a short pause so menus keep up.</summary>
    public static void Type(params ushort[] keys)
    {
        foreach (var key in keys)
        {
            Chord(key);
            Thread.Sleep(30);
        }
    }

    /// <summary>Types text as characters, so it does not depend on the keyboard layout.</summary>
    public static void TypeText(string text)
    {
        foreach (var character in text)
        {
            INPUT[] inputs = [Character(character, up: false), Character(character, up: true)];
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            Thread.Sleep(10);
        }
    }

    /// <summary>
    /// Lets go of the Windows keys if they are held, so that what is typed next is not taken as a
    /// Win shortcut. A key that does nothing is pressed first: Windows opens the Start menu when Win
    /// is released with no other key pressed since it went down, and a key swallowed by a hook does
    /// not count. Win is not pressed again afterwards: once released this way, Windows can't say
    /// whether it is still physically down.
    /// </summary>
    public static void ReleaseWin()
    {
        var held = new[] { LeftWin, RightWin }.Where(key => GetAsyncKeyState(key) < 0).ToArray();
        if (held.Length == 0)
            return;
        INPUT[] inputs = [Key(Unassigned, up: false), Key(Unassigned, up: true), .. held.Select(key => Key(key, up: true))];
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    static INPUT Character(char character, bool up) => new()
    {
        type = INPUT_KEYBOARD,
        u = new() { ki = new() { wScan = character, dwFlags = KEYEVENTF_UNICODE | (up ? KEYEVENTF_KEYUP : 0) } },
    };

    static INPUT Key(ushort key, bool up) => new()
    {
        type = INPUT_KEYBOARD,
        u = new() { ki = new() { wVk = key, dwFlags = up ? KEYEVENTF_KEYUP : 0 } },
    };

    const uint INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_KEYUP = 0x0002;
    const uint KEYEVENTF_UNICODE = 0x0004;

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

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int key);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);
}
