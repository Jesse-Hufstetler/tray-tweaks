using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;

namespace TrayTweaks.Tweaks;

/// <summary>
/// Merges File Explorer windows that sit on exactly the same screen rectangle into one window with tabs.
/// Explorer has no API for moving a tab, so each tab is reopened in the surviving window at the same
/// folder (back history and selection are not carried over) and the emptied window is closed.
/// The window at the back of a stack survives and the others are added to it from back to front, so
/// tabs run left to right in back-to-front order: the frontmost window's tabs end up rightmost.
/// </summary>
static class ExplorerTabCombiner
{
    public sealed record Result(bool TabsSupported, int WindowsClosed, int TabsMoved, int WindowsSkipped);

    public sealed record Tab(object Browser, IntPtr Window, IntPtr TabWindow, string? Path);

    const int FirstBuildWithTabs = 22621;
    const uint WM_CLOSE = 0x0010;
    const uint WM_COMMAND = 0x0111;
    // Undocumented: the command Explorer's own "new tab" button sends to the tab window.
    const int NewTabCommand = 0xA21B;
    const int DWMWA_CLOAKED = 14;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOACTIVATE = 0x0010;
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Call from an STA thread. <paramref name="include"/> can limit which windows are considered.</summary>
    public static Result CombineStacked(Func<IntPtr, bool>? include = null)
    {
        if (Environment.OSVersion.Version.Build < FirstBuildWithTabs)
            return new(false, 0, 0, 0);

        var zOrder = WindowsTopToBottom();
        var windows = ListTabs()
            .GroupBy(tab => tab.Window)
            .Where(window => IsCandidate(window.Key) && (include?.Invoke(window.Key) ?? true))
            .ToList();

        int closed = 0, moved = 0, skipped = 0;
        foreach (var stack in windows.GroupBy(window => RectOf(window.Key)).Where(stack => stack.Count() > 1))
        {
            var backToFront = stack.OrderByDescending(window => zOrder.IndexOf(window.Key)).ToList();
            var target = backToFront[0].Key;
            foreach (var source in backToFront.Skip(1))
            {
                var paths = source.Select(tab => tab.Path).ToList();
                // Only close a window once every one of its tabs is confirmed open in the target.
                if (paths.All(path => path is not null) && paths.All(path => OpenTab(target, path!)))
                {
                    // Slip the target in right behind the window it just absorbed, so the combined
                    // window ends up where the front of the stack was instead of staying at the back.
                    SetWindowPos(target, source.Key, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                    CloseWindow(source.Key);
                    closed++;
                    moved += paths.Count;
                }
                else
                {
                    skipped++;
                }
            }
        }
        return new(true, closed, moved, skipped);
    }

    public static List<Tab> ListTabs()
    {
        var tabs = new List<Tab>();
        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application")!)!;
        dynamic shellWindows = shell.Windows();
        int count = shellWindows.Count;
        for (var i = 0; i < count; i++)
        {
            try
            {
                object? browser = shellWindows.Item(i);
                if (browser is null)
                    continue;
                var window = new IntPtr(Convert.ToInt64((object)((dynamic)browser).HWND));
                if (ClassName(window) == "CabinetWClass")
                    tabs.Add(new Tab(browser, window, TabWindowOf(browser), PathOf(browser)));
            }
            catch (Exception ex) when (ex is COMException or RuntimeBinderException or InvalidCastException)
            {
                // A tab that closes mid-enumeration.
            }
        }
        return tabs;
    }

    /// <summary>Opens a new tab at <paramref name="path"/> and returns true only once that is confirmed.</summary>
    internal static bool OpenTab(IntPtr window, string path)
    {
        var tabsBefore = ListTabs().Where(tab => tab.Window == window).ToList();
        var existing = TabWindowsOf(window);
        if (existing.Count == 0)
            return false;
        PostMessage(existing[0], WM_COMMAND, NewTabCommand, IntPtr.Zero);

        var created = IntPtr.Zero;
        if (!WaitUntil(() => (created = TabWindowsOf(window).FirstOrDefault(tab => !existing.Contains(tab))) != IntPtr.Zero))
            return false;

        // Let the tab finish loading its default folder first; a navigation sent earlier gets lost or overwritten.
        Tab? newTab = null;
        if (!WaitUntil(() => (newTab = FindTab(created)) is { Path: not null } && IsIdle(newTab.Browser)))
            return false;

        try
        {
            // Virtual folders (This PC, Home) report a "::{guid}" path, which Navigate2 only takes with "shell:" in front.
            ((dynamic)newTab!.Browser).Navigate2(path.StartsWith("::") ? "shell:" + path : path);
        }
        catch (Exception ex) when (ex is COMException or RuntimeBinderException or ArgumentException)
        {
            return false;
        }
        if (!WaitUntil(() => FindTab(created) is { } tab && SamePath(tab.Path, path) && IsIdle(tab.Browser)))
            return false;

        // A misrouted navigation would have changed one of the tabs that were already there.
        var tabsNow = ListTabs();
        return tabsBefore.All(before => tabsNow.Any(now => now.TabWindow == before.TabWindow && SamePath(now.Path, before.Path)));
    }

    // On current Windows 11 builds WM_CLOSE closes only the active tab, so repeat until the window is gone.
    static void CloseWindow(IntPtr window)
    {
        var remaining = TabWindowsOf(window).Count;
        while (remaining > 0 && IsWindow(window))
        {
            var before = remaining;
            PostMessage(window, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            if (!WaitUntil(() => !IsWindow(window) || (remaining = TabWindowsOf(window).Count) < before))
                return;
        }
    }

    static Tab? FindTab(IntPtr tabWindow) => ListTabs().FirstOrDefault(tab => tab.TabWindow == tabWindow);

    static bool SamePath(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    static bool IsIdle(object browser)
    {
        try
        {
            dynamic b = browser;
            return !(bool)b.Busy && (int)b.ReadyState == 4;
        }
        catch (Exception ex) when (ex is COMException or RuntimeBinderException or InvalidCastException)
        {
            return false;
        }
    }

    // Newest tab first.
    static List<IntPtr> TabWindowsOf(IntPtr window)
    {
        var tabs = new List<IntPtr>();
        var tab = IntPtr.Zero;
        while ((tab = FindWindowEx(window, tab, "ShellTabWindowClass", null)) != IntPtr.Zero)
            tabs.Add(tab);
        return tabs;
    }

    static bool WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                return false;
            Thread.Sleep(50);
        }
        return true;
    }

    static string? PathOf(object browser)
    {
        try
        {
            return ((dynamic)browser).Document.Folder.Self.Path as string;
        }
        catch (Exception ex) when (ex is COMException or RuntimeBinderException)
        {
            return null;
        }
    }

    // Every tab reports the same top-level HWND, so this per-tab child window is what tells tabs apart.
    static IntPtr TabWindowOf(object browser)
    {
        var service = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837"); // SID_STopLevelBrowser
        var iid = new Guid("000214E2-0000-0000-C000-000000000046"); // IID_IShellBrowser
        if (browser is IComServiceProvider provider
            && provider.QueryService(ref service, ref iid, out var shellBrowser) == 0
            && shellBrowser is IOleWindow oleWindow
            && oleWindow.GetWindow(out var hwnd) == 0)
            return hwnd;
        return IntPtr.Zero;
    }

    static bool IsCandidate(IntPtr window) =>
        IsWindowVisible(window)
        && !IsIconic(window)
        && !IsCloaked(window)
        && FindWindowEx(window, IntPtr.Zero, "ShellTabWindowClass", null) != IntPtr.Zero;

    // Windows on another virtual desktop are cloaked rather than hidden.
    static bool IsCloaked(IntPtr window) =>
        DwmGetWindowAttribute(window, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0;

    static (int, int, int, int) RectOf(IntPtr window)
    {
        GetWindowRect(window, out var rect);
        return (rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    static string ClassName(IntPtr window)
    {
        var name = new StringBuilder(256);
        GetClassName(window, name, name.Capacity);
        return name.ToString();
    }

    static List<IntPtr> WindowsTopToBottom()
    {
        var windows = new List<IntPtr>();
        EnumWindows((window, _) =>
        {
            windows.Add(window);
            return true;
        }, IntPtr.Zero);
        return windows;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    delegate bool EnumWindowsProc(IntPtr window, IntPtr lParam);

    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IComServiceProvider
    {
        [PreserveSig]
        int QueryService(ref Guid service, ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object result);
    }

    [ComImport, Guid("00000114-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IOleWindow
    {
        [PreserveSig]
        int GetWindow(out IntPtr window);
    }

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(IntPtr window, StringBuilder name, int maxCount);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr window, out RECT rect);

    [DllImport("user32.dll")]
    static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    static extern bool IsWindow(IntPtr window);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("dwmapi.dll")]
    static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);
}
