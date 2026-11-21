using System.Diagnostics;
using Microsoft.Win32;
using TrayTweaks.Input;

namespace TrayTweaks.Tweaks;

/// <summary>
/// Starts classic Outlook whenever it isn't running. Outlook is started without administrator
/// rights, because an elevated Outlook misbehaves.
/// </summary>
public sealed class KeepOutlookRunningTweak : ToggleTweak
{
    static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    ManualResetEvent? _stop;

    public override string Id => "keep-outlook-running";
    public override string Name => "Keep Outlook running";
    public override string Description => "Starts Outlook again whenever it isn't running, without administrator rights.";
    public override string Glyph => "";

    public static string? OutlookPath
    {
        get
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE");
            return key?.GetValue(null) as string;
        }
    }

    public static bool IsOutlookRunning()
    {
        var session = Process.GetCurrentProcess().SessionId;
        var processes = Process.GetProcessesByName("OUTLOOK");
        try
        {
            return processes.Any(process => process.SessionId == session);
        }
        finally
        {
            foreach (var process in processes)
                process.Dispose();
        }
    }

    public override void Enable()
    {
        if (_stop is not null)
            return;
        var path = OutlookPath ?? throw new InvalidOperationException("Outlook isn't installed on this computer.");

        var stop = _stop = new ManualResetEvent(false);
        var thread = new Thread(() =>
        {
            do
            {
                try
                {
                    if (!IsOutlookRunning())
                        UnelevatedLauncher.Start(path);
                }
                catch
                {
                    // Try again on the next round.
                }
            }
            while (!stop.WaitOne(Interval));
        })
        {
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    public override void Disable()
    {
        _stop?.Set();
        _stop = null;
    }
}
