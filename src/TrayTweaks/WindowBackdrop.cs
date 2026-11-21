using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace TrayTweaks;

public static class WindowBackdrop
{
    /// <summary>
    /// Replaces the Fluent theme's Mica with plain blurred glass: whatever is behind the window,
    /// blurred, with no tint, so it can come out light or dark. Windows' own backdrops can't be
    /// adjusted and add a milky layer, so this uses the undocumented accent policy, which may stop
    /// working after a Windows update.
    /// Needs Windows 11 22H2; earlier versions keep their solid background.
    /// </summary>
    public static void UseGlass(Window window)
    {
        // WPF sets Mica and its frame again at several points (window creation, theme change),
        // so reapply rather than set once.
        window.SourceInitialized += (_, _) => Apply(window);
        window.ContentRendered += (_, _) => Apply(window);
        window.Activated += (_, _) => Apply(window);
    }

    static void Apply(Window window)
    {
        if (Environment.OSVersion.Version.Build < 22621 || SystemParameters.HighContrast || !TransparencyEffectsOn)
            return;
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;

        // The accent only shows through once the system backdrop is off and the frame
        // WPF extended over the whole window is taken back.
        var noBackdrop = DWMSBT_NONE;
        DwmSetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, ref noBackdrop, sizeof(int));
        var margins = new MARGINS();
        DwmExtendFrameIntoClientArea(handle, ref margins);

        // Flags must be 0: with the usual 2 (use the gradient colour) this state renders black.
        var accent = new ACCENT_POLICY { AccentState = ACCENT_ENABLE_BLURBEHIND, AccentFlags = 0 };
        var size = Marshal.SizeOf<ACCENT_POLICY>();
        var memory = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, memory, false);
            var data = new WINDOWCOMPOSITIONATTRIBDATA { Attrib = WCA_ACCENT_POLICY, Data = memory, Size = size };
            SetWindowCompositionAttribute(handle, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
    }

    static bool TransparencyEffectsOn
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("EnableTransparency") is not 0;
        }
    }

    const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    const int DWMSBT_NONE = 1;
    const int WCA_ACCENT_POLICY = 19;
    const int ACCENT_ENABLE_BLURBEHIND = 3;

    [StructLayout(LayoutKind.Sequential)]
    struct ACCENT_POLICY
    {
        public int AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WINDOWCOMPOSITIONATTRIBDATA
    {
        public int Attrib;
        public IntPtr Data;
        public int Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MARGINS
    {
        public int Left, Right, Top, Bottom;
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

    [DllImport("user32.dll")]
    static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WINDOWCOMPOSITIONATTRIBDATA data);
}
