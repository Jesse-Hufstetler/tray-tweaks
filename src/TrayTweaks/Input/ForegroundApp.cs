using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TrayTweaks.Input;

public static class ForegroundApp
{
    /// <summary>The focused window's exe name without ".exe" (e.g. "chrome"), or null when it can't be read.</summary>
    public static string? ProcessName()
    {
        var window = GetForegroundWindow();
        if (window == IntPtr.Zero)
            return null;

        GetWindowThreadProcessId(window, out var processId);
        var process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (process == IntPtr.Zero)
            return null;

        try
        {
            var path = new StringBuilder(1024);
            var length = path.Capacity;
            return QueryFullProcessImageName(process, 0, path, ref length)
                ? Path.GetFileNameWithoutExtension(path.ToString())
                : null;
        }
        finally
        {
            CloseHandle(process);
        }
    }

    const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(uint access, bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder exeName, ref int size);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}
