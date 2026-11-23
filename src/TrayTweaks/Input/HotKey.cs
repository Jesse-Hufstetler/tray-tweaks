using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace TrayTweaks.Input;

/// <summary>
/// A system-wide key that runs an action, registered with Windows (no keyboard hook, so the app
/// never sees any other key). Create and dispose it on the UI thread; the action runs there.
/// </summary>
public sealed class HotKey : IDisposable
{
    const int WM_HOTKEY = 0x0312;
    const uint MOD_NOREPEAT = 0x4000;

    static readonly Dictionary<int, Action> Handlers = [];
    static int _lastId = 0x5400;

    readonly int _id;

    public HotKey(ushort key, string keyName, Action pressed)
    {
        _id = ++_lastId;
        if (!RegisterHotKey(IntPtr.Zero, _id, MOD_NOREPEAT, key))
            throw new InvalidOperationException($"{keyName} could not be claimed; another program is probably using it.", new Win32Exception());

        if (Handlers.Count == 0)
            ComponentDispatcher.ThreadPreprocessMessage += OnThreadMessage;
        Handlers[_id] = pressed;
    }

    public void Dispose()
    {
        if (!Handlers.Remove(_id))
            return;
        UnregisterHotKey(IntPtr.Zero, _id);
        if (Handlers.Count == 0)
            ComponentDispatcher.ThreadPreprocessMessage -= OnThreadMessage;
    }

    static void OnThreadMessage(ref MSG message, ref bool handled)
    {
        if (message.message != WM_HOTKEY || !Handlers.TryGetValue((int)message.wParam, out var pressed))
            return;
        handled = true;
        pressed();
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);

    [DllImport("user32.dll")]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
