using System.Windows.Threading;
using TrayTweaks.Tweaks;

namespace TrayTweaks;

public sealed class TweakManager
{
    static readonly CombineExplorerWindowsTweak CombineExplorerWindows = new();

    public static readonly IReadOnlyList<Tweak> AllTweaks =
    [
        CombineExplorerWindows,
        new ForwardButtonCloseTweak(),
        new BackButtonTweak(CombineExplorerWindows),
        new KeepOutlookRunningTweak(),
    ];

    readonly AppSettings _settings = AppSettings.Load();
    readonly HashSet<string> _runningActions = [];

    public event Action<Tweak, Exception>? TweakFailed;
    public event Action<Tweak, string>? ActionFinished;
    public event Action? Changed;

    static IEnumerable<ToggleTweak> Toggles => AllTweaks.OfType<ToggleTweak>();

    public bool IsEnabled(ToggleTweak tweak) => _settings.EnabledTweaks.Contains(tweak.Id);

    public void StartEnabledTweaks()
    {
        foreach (var tweak in Toggles)
            tweak.RunAction = Run;
        foreach (var tweak in Toggles.Where(IsEnabled))
            Try(tweak, tweak.Enable);
    }

    public void StopAll()
    {
        foreach (var tweak in Toggles.Where(IsEnabled))
            Try(tweak, tweak.Disable);
    }

    public void SetEnabled(ToggleTweak tweak, bool enabled)
    {
        if (enabled == IsEnabled(tweak))
            return;

        if (!Try(tweak, enabled ? tweak.Enable : tweak.Disable) && enabled)
            return;

        if (enabled)
            _settings.EnabledTweaks.Add(tweak.Id);
        else
            _settings.EnabledTweaks.Remove(tweak.Id);
        _settings.Save();
        Changed?.Invoke();
    }

    /// <summary>Runs the action off the UI thread; a second click while it is still running is ignored.</summary>
    public void Run(ActionTweak tweak)
    {
        if (!_runningActions.Add(tweak.Id))
            return;

        var ui = Dispatcher.CurrentDispatcher;
        var thread = new Thread(() =>
        {
            string? message = null;
            Exception? error = null;
            try
            {
                message = tweak.Run();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            ui.BeginInvoke(() =>
            {
                _runningActions.Remove(tweak.Id);
                if (error is not null)
                    TweakFailed?.Invoke(tweak, error);
                else if (message is not null)
                    ActionFinished?.Invoke(tweak, message);
            });
        })
        {
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    bool Try(Tweak tweak, Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            TweakFailed?.Invoke(tweak, ex);
            return false;
        }
    }
}
