namespace TrayTweaks;

public sealed class TrayContext : ApplicationContext
{
    readonly TweakManager _tweaks = new();
    readonly NotifyIcon _trayIcon;
    SettingsForm? _settingsForm;

    public TrayContext()
    {
        var menu = new ContextMenuStrip();
        menu.Opening += (_, _) => BuildMenu(menu);

        _trayIcon = new NotifyIcon
        {
            Icon = AppIcon.Load(SystemInformation.SmallIconSize),
            Text = "TrayTweaks",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _trayIcon.DoubleClick += (_, _) => ShowSettings();

        _tweaks.TweakFailed += (tweak, ex) =>
            _trayIcon.ShowBalloonTip(5000, $"{tweak.Name} failed", ex.Message, ToolTipIcon.Error);
        _tweaks.StartEnabledTweaks();
    }

    void BuildMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();
        foreach (var tweak in TweakManager.AllTweaks)
        {
            var item = new ToolStripMenuItem(tweak.Name)
            {
                Checked = _tweaks.IsEnabled(tweak),
                ToolTipText = tweak.Description,
            };
            item.Click += (_, _) => _tweaks.SetEnabled(tweak, !_tweaks.IsEnabled(tweak));
            menu.Items.Add(item);
        }
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings...", null, (_, _) => ShowSettings());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
    }

    void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }
        _settingsForm = new SettingsForm(_tweaks);
        _settingsForm.Show();
    }

    protected override void ExitThreadCore()
    {
        _tweaks.StopAll();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.ExitThreadCore();
    }
}

static class AppIcon
{
    public static Icon Load(Size size)
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("app.ico")!;
        return new Icon(stream, size);
    }
}
