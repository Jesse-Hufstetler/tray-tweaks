using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TrayTweaks.Mouse;

public enum SideButton
{
    Back = 1,
    Forward = 2,
}

/// <summary>
/// Watches the Back and Forward mouse buttons system-wide. Create and dispose it on a thread that
/// pumps messages (the UI thread); the handler runs there and must return quickly.
/// </summary>
public sealed class MouseHook : IDisposable
{
    const int WH_MOUSE_LL = 14;
    const int WM_XBUTTONDOWN = 0x020B;
    const int WM_XBUTTONUP = 0x020C;

    readonly Func<SideButton, bool> _onDown;
    readonly HookProc _proc;
    readonly HashSet<SideButton> _swallowed = [];
    IntPtr _hook;

    /// <param name="onDown">Called when a side button goes down. Return true to hide the press from other apps.</param>
    public MouseHook(Func<SideButton, bool> onDown)
    {
        _onDown = onDown;
        _proc = Callback;
        _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(null), 0);
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
        if (code >= 0 && (wParam == WM_XBUTTONDOWN || wParam == WM_XBUTTONUP))
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var button = (SideButton)(data.mouseData >> 16);
            if ((button is SideButton.Back or SideButton.Forward) && Handle(button, wParam == WM_XBUTTONDOWN))
                return 1;
        }
        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    // A swallowed press must have its release swallowed too, or the app sees a release with no press.
    bool Handle(SideButton button, bool down)
    {
        if (!down)
            return _swallowed.Remove(button);

        bool swallow;
        try
        {
            swallow = _onDown(button);
        }
        catch
        {
            swallow = false;
        }
        if (swallow)
            _swallowed.Add(button);
        return swallow;
    }

    delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    struct MSLLHOOKSTRUCT
    {
        public int x;
        public int y;
        public uint mouseData;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr GetModuleHandle(string? lpModuleName);
}
