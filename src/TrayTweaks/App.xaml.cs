using System.Security.Principal;
using System.Windows;
using TrayTweaks.Secrets;

namespace TrayTweaks;

public partial class App : Application
{
    Mutex? _singleInstance;
    TrayIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(true, @"Local\TrayTweaks.SingleInstance", out var isFirst);
        if (!isFirst)
        {
            Shutdown();
            return;
        }

        _tray = new TrayIcon();

        // A secret saved by a copy started from another app's sandbox, or under another account,
        // is invisible or unreadable here, and nothing else would say so.
        using var identity = WindowsIdentity.GetCurrent();
        var readable = SecretStore.Slots.Select(slot => $"{slot.Id}={(SecretStore.Default.Has(slot.Id) ? SecretStore.Default.Get(slot.Id) is null ? "unreadable" : "ok" : "not set")}");
        AppLog.Write($"Started as {identity.Name}, admin={new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)}, logon type {identity.AuthenticationType}, secrets: {string.Join(", ", readable)}");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
