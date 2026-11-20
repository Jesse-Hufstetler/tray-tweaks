using TrayTweaks.Tweaks;

namespace TrayTweaks;

public sealed class TweakManager
{
    public static readonly IReadOnlyList<Tweak> AllTweaks =
    [
        new KeepAwakeTweak(),
    ];

    readonly AppSettings _settings = AppSettings.Load();

    public event Action<Tweak, Exception>? TweakFailed;

    public bool IsEnabled(Tweak tweak) => _settings.EnabledTweaks.Contains(tweak.Id);

    public void StartEnabledTweaks()
    {
        foreach (var tweak in AllTweaks.Where(IsEnabled))
            Run(tweak, tweak.Enable);
    }

    public void StopAll()
    {
        foreach (var tweak in AllTweaks.Where(IsEnabled))
            Run(tweak, tweak.Disable);
    }

    public void SetEnabled(Tweak tweak, bool enabled)
    {
        if (enabled == IsEnabled(tweak))
            return;

        if (!Run(tweak, enabled ? tweak.Enable : tweak.Disable) && enabled)
            return;

        if (enabled)
            _settings.EnabledTweaks.Add(tweak.Id);
        else
            _settings.EnabledTweaks.Remove(tweak.Id);
        _settings.Save();
    }

    bool Run(Tweak tweak, Action action)
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
