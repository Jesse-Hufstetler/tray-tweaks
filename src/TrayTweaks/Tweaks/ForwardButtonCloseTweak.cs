using TrayTweaks.Input;
using TrayTweaks.Mouse;

namespace TrayTweaks.Tweaks;

public enum CloseAction
{
    /// <summary>Ctrl+W.</summary>
    CloseTab,
    /// <summary>Ctrl+F4.</summary>
    CloseDocument,
    /// <summary>Alt+F4.</summary>
    CloseWindow,
    VbsEditCloseFile,
    QuickBooksPosClose,
    /// <summary>Leave the Forward button alone.</summary>
    PassThrough,
}

/// <summary>
/// Makes the mouse's Forward button close things. The per-app table was carried over from the
/// XButton2 hotkey in the AutoHotkey script this app replaces.
/// </summary>
public sealed class ForwardButtonCloseTweak : ToggleTweak
{
    static readonly Dictionary<string, CloseAction> Apps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chrome"] = CloseAction.CloseTab,
        ["brave"] = CloseAction.CloseTab,
        ["firefox"] = CloseAction.CloseTab,
        ["msedge"] = CloseAction.CloseTab,
        ["vivaldi"] = CloseAction.CloseTab,
        ["iexplore"] = CloseAction.CloseTab,
        ["explorer"] = CloseAction.CloseTab,
        ["code"] = CloseAction.CloseTab,
        ["zed"] = CloseAction.CloseTab,
        ["sublime_text"] = CloseAction.CloseTab,
        ["notepad++"] = CloseAction.CloseTab,
        ["linqpad"] = CloseAction.CloseTab,
        ["linqpad8"] = CloseAction.CloseTab,
        ["linqpad9"] = CloseAction.CloseTab,
        ["azuredatastudio"] = CloseAction.CloseTab,
        ["SumatraPDF"] = CloseAction.CloseTab,
        ["BCompare"] = CloseAction.CloseTab,
        ["claude"] = CloseAction.CloseTab,

        ["ssms"] = CloseAction.CloseDocument,
        ["devenv"] = CloseAction.CloseDocument,
        ["webstorm64"] = CloseAction.CloseDocument,

        ["vbsedit"] = CloseAction.VbsEditCloseFile,
        ["qbposshell"] = CloseAction.QuickBooksPosClose,

        // Remote-control tools: the button belongs to the other computer.
        ["TeamViewer"] = CloseAction.PassThrough,
        ["synergys"] = CloseAction.PassThrough,
    };

    MouseHook? _hook;

    public override string Id => "forward-button-close";
    public override string Name => "Forward button closes tabs";
    public override string Description => "The mouse's Forward button closes the current tab, or the whole window in apps without tabs.";
    public override string Glyph => "";

    public override void Enable() => _hook ??= new MouseHook(OnSideButton);

    public override void Disable()
    {
        _hook?.Dispose();
        _hook = null;
    }

    /// <summary>What Forward does in the app with this exe name (no ".exe"); null means the app could not be identified.</summary>
    public static CloseAction ActionFor(string? processName) =>
        string.IsNullOrEmpty(processName) ? CloseAction.PassThrough
        : Apps.TryGetValue(processName, out var action) ? action
        : CloseAction.CloseWindow;

    static bool OnSideButton(SideButton button)
    {
        if (button != SideButton.Forward)
            return false;

        var action = ActionFor(ForegroundApp.ProcessName());
        if (action == CloseAction.PassThrough)
            return false;

        // Off the hook's thread: Windows drops a hook that is slow to answer.
        Task.Run(() => Send(action));
        return true;
    }

    static void Send(CloseAction action)
    {
        switch (action)
        {
            case CloseAction.CloseTab:
                Keyboard.Chord(Keyboard.Control, Keyboard.Letter('w'));
                break;
            case CloseAction.CloseDocument:
                Keyboard.Chord(Keyboard.Control, Keyboard.F4);
                break;
            case CloseAction.CloseWindow:
                Keyboard.Chord(Keyboard.Alt, Keyboard.F4);
                break;
            case CloseAction.VbsEditCloseFile:
                // File menu > Close, then Enter for the prompt that follows.
                Keyboard.Chord(Keyboard.Alt, Keyboard.Letter('f'));
                Thread.Sleep(50);
                Keyboard.Chord(Keyboard.Letter('c'));
                Thread.Sleep(50);
                Keyboard.Chord(Keyboard.Enter);
                break;
            case CloseAction.QuickBooksPosClose:
                Keyboard.Chord(Keyboard.Alt, Keyboard.F4);
                Thread.Sleep(100);
                Keyboard.Chord(Keyboard.Enter);
                Thread.Sleep(10);
                Keyboard.Chord(Keyboard.Enter);
                break;
        }
    }
}
