using System.Security.Principal;
using Microsoft.Win32;

namespace TrayTweaks;

/// <summary>
/// "Start at sign-in" via a Task Scheduler logon task that runs with highest privileges. A Run-key
/// entry does not work for this app: Windows skips sign-in programs that need elevation.
/// </summary>
public static class StartupRegistration
{
    const string TaskName = "TrayTweaks";

    // Task Scheduler 2.0 constants.
    const int LogonTrigger = 9;
    const int ExecAction = 0;
    const int InteractiveToken = 3;
    const int HighestRunLevel = 1;
    const int CreateOrUpdate = 6;

    public static bool IsEnabled
    {
        get
        {
            try
            {
                Connect().GetFolder(@"\").GetTask(TaskName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static void SetEnabled(bool enabled)
    {
        var service = Connect();
        var folder = service.GetFolder(@"\");
        if (enabled)
            folder.RegisterTaskDefinition(TaskName, NewDefinition(service), CreateOrUpdate, null, null, InteractiveToken, null);
        else if (IsEnabled)
            folder.DeleteTask(TaskName, 0);

        RemoveOldRunValue();
    }

    /// <summary>The task as Task Scheduler XML, for checking what would be registered.</summary>
    public static string TaskXml => NewDefinition(Connect()).XmlText;

    static dynamic Connect()
    {
        dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")!)!;
        service.Connect();
        return service;
    }

    static dynamic NewDefinition(dynamic service)
    {
        string user = WindowsIdentity.GetCurrent().Name;
        var task = service.NewTask(0);
        task.RegistrationInfo.Description = "Starts TrayTweaks when you sign in. Turn it off in TrayTweaks Settings.";
        task.Principal.UserId = user;
        task.Principal.LogonType = InteractiveToken;
        task.Principal.RunLevel = HighestRunLevel;

        // The defaults would keep it from starting on battery, end it after three days,
        // and run it below normal priority, which makes the mouse hook lag.
        task.Settings.DisallowStartIfOnBatteries = false;
        task.Settings.StopIfGoingOnBatteries = false;
        task.Settings.ExecutionTimeLimit = "PT0S";
        task.Settings.Priority = 4;

        var trigger = task.Triggers.Create(LogonTrigger);
        trigger.UserId = user;
        var action = task.Actions.Create(ExecAction);
        action.Path = Environment.ProcessPath;
        return task;
    }

    // Versions up to 0.1.0 registered here; the entry never worked once the app required admin.
    static void RemoveOldRunValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        key?.DeleteValue("TrayTweaks", throwOnMissingValue: false);
    }
}
