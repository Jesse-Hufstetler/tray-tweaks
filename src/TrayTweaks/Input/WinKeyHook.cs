using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace TrayTweaks.Input;

/// <summary>
/// Takes over Win + one key, for the combinations Windows keeps for itself (the Win + arrow keys
/// can't be claimed with <see cref="HotKey"/>). It is a low-level keyboard hook, so every keystroke
/// passes through <see cref="Callback"/>; it looks only at the one key, and only while the Windows
/// key is held with no Ctrl, Alt or Shift, and it keeps nothing. Prefer <see cref="HotKey"/>
/// wherever Windows allows it. Create and dispose it on the UI thread; the action runs there.
/// </summary>
public sealed class WinKeyHook : IDisposable
{
    const int WH_KEYBOARD_LL = 13;
    const int WM_KEYDOWN = 0x0100;
    const int WM_SYSKEYDOWN = 0x0104;

    readonly uint _key;
    readonly Action _pressed;
    readonly HookProc _proc;
    readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    bool _swallowing;
    IntPtr _hook;

    public WinKeyHook(ushort key, Action pressed)
    {
        _key = key;
        _pressed = pressed;
        _proc = Callback;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        if (_hook == IntPtr.Zero)
            throw new Win32Exception();
    }

    public void Dispose()
    {
        if (_hook == IntPtr.Zero)
            return;
        UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }

    IntPtr Callback(int code, IntPtr wParam, IntPtr lParam)
    {
        // The key code is the first field of the structure lParam points to.
        if (code >= 0 && (uint)Marshal.ReadInt32(lParam) == _key && Handle(wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
            return 1;
        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    // A swallowed press has its repeats and its release swallowed too, so no app sees half a key.
    bool Handle(bool down)
    {
        if (!down)
        {
            var swallowed = _swallowing;
            _swallowing = false;
            return swallowed;
        }
        if (_swallowing)
            return true;
        if (!(Held(Keyboard.LeftWin) || Held(Keyboard.RightWin)) || Held(Keyboard.Control) || Held(Keyboard.Alt) || Held(Keyboard.Shift))
            return false;

        _swallowing = true;
        // Not run here: Windows drops a hook whose callback is slow.
        _dispatcher.BeginInvoke(_pressed);
        return true;
    }

    static bool Held(ushort key) => GetAsyncKeyState(key) < 0;

    delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int key);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr GetModuleHandle(string? lpModuleName);
}
