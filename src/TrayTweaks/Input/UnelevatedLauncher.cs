using System.Runtime.InteropServices;

namespace TrayTweaks.Input;

/// <summary>
/// Starts programs without administrator rights even though this app has them, by asking the
/// desktop's Explorer process to do the launch (the approach Microsoft documents for this).
/// Call it from an STA thread.
/// </summary>
public static class UnelevatedLauncher
{
    const int SWC_DESKTOP = 8;
    const int SWFO_NEEDDISPATCH = 1;
    const uint SVGIO_BACKGROUND = 0;
    const int SW_SHOWNORMAL = 1;

    /// <returns>False when Explorer's desktop isn't available to launch through, e.g. early in sign-in.</returns>
    public static bool Start(string file, string arguments = "")
    {
        try
        {
            dynamic shellWindows = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"))!)!;
            var hwnd = 0;
            object? desktop = shellWindows.FindWindowSW(0, "", SWC_DESKTOP, ref hwnd, SWFO_NEEDDISPATCH);
            if (desktop is not IComServiceProvider provider)
                return false;

            var service = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837"); // SID_STopLevelBrowser
            var browserId = typeof(IShellBrowser).GUID;
            if (provider.QueryService(ref service, ref browserId, out var browser) != 0)
                return false;
            if (((IShellBrowser)browser).QueryActiveShellView(out var view) != 0)
                return false;

            var dispatchId = new Guid("00020400-0000-0000-C000-000000000046"); // IID_IDispatch
            if (view.GetItemObject(SVGIO_BACKGROUND, ref dispatchId, out var background) != 0)
                return false;

            // The background object lives in Explorer, so its ShellExecute runs there, at Explorer's level.
            dynamic shell = ((dynamic)background).Application;
            shell.ShellExecute(file, arguments, "", "open", SW_SHOWNORMAL);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }

    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IComServiceProvider
    {
        [PreserveSig]
        int QueryService(ref Guid service, ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object result);
    }

    // Only the methods used are named; the rest hold their place in the interface's method order.
    [ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellBrowser
    {
        void GetWindow();
        void ContextSensitiveHelp();
        void InsertMenusSB();
        void SetMenuSB();
        void RemoveMenusSB();
        void SetStatusTextSB();
        void EnableModelessSB();
        void TranslateAcceleratorSB();
        void BrowseObject();
        void GetViewStateStream();
        void GetControlWindow();
        void SendControlMsg();
        [PreserveSig]
        int QueryActiveShellView(out IShellView view);
    }

    [ComImport, Guid("000214E3-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellView
    {
        void GetWindow();
        void ContextSensitiveHelp();
        void TranslateAccelerator();
        void EnableModeless();
        void UIActivate();
        void Refresh();
        void CreateViewWindow();
        void DestroyViewWindow();
        void GetCurrentInfo();
        void AddPropertySheetPages();
        void SaveViewState();
        void SelectItem();
        [PreserveSig]
        int GetItemObject(uint item, ref Guid iid, [MarshalAs(UnmanagedType.IDispatch)] out object result);
    }
}
