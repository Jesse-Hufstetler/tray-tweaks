using TrayTweaks.Input;
using TrayTweaks.Mouse;
using TrayTweaks.Secrets;

namespace TrayTweaks.Tweaks;

public enum BackAction
{
    /// <summary>Run Combine stacked Explorer windows.</summary>
    CombineExplorerWindows,
    /// <summary>Select everything, autofit row heights and column widths, go to the first cell.</summary>
    ExcelAutofit,
    /// <summary>Type the stored login into a "Connect" window.</summary>
    ConnectLogin,
    /// <summary>Leave the Back button alone.</summary>
    PassThrough,
}

/// <summary>
/// Gives the mouse's Back button jobs of its own. Carried over from the XButton1 hotkey in the
/// AutoHotkey script this app replaces, except that the fallback there was Show desktop.
/// </summary>
public sealed class BackButtonTweak(ActionTweak combineExplorerWindows) : ToggleTweak
{
    public static readonly SecretSlot ConnectLogin = new(
        "connect-login",
        "Connect window login",
        "Back button, in a window with \"Connect\" in its title: Tab, this text, Enter three times.");

    MouseHook? _hook;

    public override string Id => "back-button";
    public override string Name => "Back button combines Explorer windows";
    public override string Description => "The mouse's Back button combines stacked Explorer windows. In Excel it autofits every row and column, and in a \"Connect\" window it types the stored login.";
    public override string Glyph => "";

    public override void Enable() => _hook ??= new MouseHook(OnSideButton);

    public override void Disable()
    {
        _hook?.Dispose();
        _hook = null;
    }

    /// <summary>What Back does, given the focused window's exe name (no ".exe", null if unknown) and title.</summary>
    public static BackAction ActionFor(string? processName, string windowTitle)
    {
        if (string.IsNullOrEmpty(processName))
            return BackAction.PassThrough;
        if (processName.Equals("excel", StringComparison.OrdinalIgnoreCase))
            return BackAction.ExcelAutofit;
        // Same rule as the script: any window whose title contains "Connect", case-sensitive.
        if (windowTitle.Contains("Connect", StringComparison.Ordinal))
            return BackAction.ConnectLogin;
        return BackAction.CombineExplorerWindows;
    }

    bool OnSideButton(SideButton button)
    {
        if (button != SideButton.Back)
            return false;

        switch (ActionFor(ForegroundApp.ProcessName(), ForegroundApp.WindowTitle()))
        {
            case BackAction.CombineExplorerWindows:
                RunAction?.Invoke(combineExplorerWindows);
                return true;
            case BackAction.ExcelAutofit:
                // Off the hook's thread: Windows drops a hook that is slow to answer.
                Task.Run(ExcelAutofit);
                return true;
            case BackAction.ConnectLogin:
                // With nothing stored, the button is left alone.
                if (SecretStore.Default.Get(ConnectLogin.Id) is not { Length: > 0 } login)
                    return false;
                Task.Run(() => TypeLogin(login));
                return true;
            default:
                return false;
        }
    }

    static void ExcelAutofit()
    {
        Keyboard.Chord(Keyboard.Control, Keyboard.Letter('a'));
        Thread.Sleep(30);
        // Ribbon keys: Home > Format > AutoFit Row Height, then AutoFit Column Width.
        Keyboard.Type(Keyboard.Alt, Keyboard.Letter('h'), Keyboard.Letter('o'), Keyboard.Letter('a'));
        Keyboard.Type(Keyboard.Alt, Keyboard.Letter('h'), Keyboard.Letter('o'), Keyboard.Letter('i'));
        Keyboard.Chord(Keyboard.Control, Keyboard.Home);
    }

    /// <summary>Tab to the password box, type, then Enter three times for the prompts that follow.</summary>
    public static void TypeLogin(string login)
    {
        Keyboard.Type(Keyboard.Tab);
        Keyboard.TypeText(login);
        Thread.Sleep(30);
        Keyboard.Type(Keyboard.Enter, Keyboard.Enter, Keyboard.Enter);
    }
}
