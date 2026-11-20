using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace TrayTweaks;

public sealed class TrayIcon : IDisposable
{
    readonly TweakManager _tweaks = new();
    readonly Forms.NotifyIcon _icon;
    SettingsWindow? _settings;

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    public TrayIcon()
    {
        using var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/app.ico")).Stream;
        _icon = new Forms.NotifyIcon
        {
            Icon = new System.Drawing.Icon(iconStream, Forms.SystemInformation.SmallIconSize),
            Text = "TrayTweaks",
            Visible = true,
        };
        _icon.MouseUp += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Right)
                ShowMenu();
            else if (e.Button == Forms.MouseButtons.Left)
                ShowSettings();
        };

        _tweaks.TweakFailed += (tweak, ex) =>
            _icon.ShowBalloonTip(5000, $"{tweak.Name} failed", ex.Message, Forms.ToolTipIcon.Error);
        _tweaks.ActionFinished += (tweak, message) =>
            _icon.ShowBalloonTip(5000, tweak.Name, message, Forms.ToolTipIcon.Info);
        _tweaks.StartEnabledTweaks();
    }

    void ShowMenu()
    {
        var menu = new ContextMenu { Placement = PlacementMode.MousePoint };

        foreach (var action in TweakManager.AllTweaks.OfType<ActionTweak>())
        {
            var item = CommandItem(action.Name, action.Glyph, () => _tweaks.Run(action));
            item.ToolTip = action.Description;
            menu.Items.Add(item);
        }

        foreach (var toggle in TweakManager.AllTweaks.OfType<ToggleTweak>())
        {
            // Not IsCheckable: the Fluent theme gives checkable items their own column,
            // which leaves the icons of the other items indented. The check goes in the icon slot instead.
            var enabled = _tweaks.IsEnabled(toggle);
            var item = new MenuItem
            {
                Header = toggle.Name,
                Icon = enabled ? GlyphIcon("") : null,
                ToolTip = toggle.Description,
            };
            AutomationProperties.SetItemStatus(item, enabled ? "On" : "Off");
            item.Click += (_, _) => _tweaks.SetEnabled(toggle, !enabled);
            menu.Items.Add(item);
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(CommandItem("Settings", "", ShowSettings));
        menu.Items.Add(CommandItem("Exit", "", () => Application.Current.Shutdown()));

        // A menu opened from the tray only closes on an outside click if it owns the foreground.
        menu.Opened += (_, _) =>
        {
            if (PresentationSource.FromVisual(menu) is HwndSource source)
                SetForegroundWindow(source.Handle);
        };
        menu.IsOpen = true;
    }

    static MenuItem CommandItem(string text, string glyph, Action action)
    {
        var item = new MenuItem { Header = text, Icon = GlyphIcon(glyph) };
        item.Click += (_, _) => action();
        return item;
    }

    static TextBlock GlyphIcon(string glyph) => new()
    {
        Text = glyph,
        FontFamily = (FontFamily)Application.Current.FindResource("IconFont"),
        FontSize = 16,
        Width = 16,
        TextAlignment = TextAlignment.Center,
    };

    void ShowSettings()
    {
        if (_settings is null)
        {
            _settings = new SettingsWindow(_tweaks);
            _settings.Closed += (_, _) => _settings = null;
            _settings.Show();
        }
        if (_settings.WindowState == WindowState.Minimized)
            _settings.WindowState = WindowState.Normal;
        _settings.Activate();
    }

    public void Dispose()
    {
        _tweaks.StopAll();
        _icon.Visible = false;
        _icon.Dispose();
    }
}
