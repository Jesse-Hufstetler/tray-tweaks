# TrayTweaks

A small Windows system tray utility that holds my personal Windows tweaks and automations. Each tweak can be run or switched on and off from the tray menu or the Settings window, and the app can start when I sign in. Runs on Windows 10 and 11.

## Build and run

Needs the .NET 10 SDK. The app always runs as administrator (its manifest requires it), so run this from an elevated terminal; from a normal one it fails with "The requested operation requires elevation".

```powershell
dotnet run --project src\TrayTweaks
```

Click the tray icon to open Settings. Right-click it to run or toggle tweaks, open Settings or exit.

## Tweaks

- **Combine stacked Explorer windows** (action, Windows 11 22H2 or later): merges File Explorer windows that sit on exactly the same spot, for example in the same FancyZones zone, into one window with tabs. Explorer has no API for moving a tab, so each tab is reopened in the surviving window at the same folder and the emptied window is closed; a tab's back history and selection are not carried over. The window at the back of the stack survives and the others are added to it from back to front, so the frontmost window's tabs end up rightmost. A window is only closed once all of its tabs are confirmed open in the surviving window. It works silently; a notification appears only if a window had to be left alone.
- **Forward button closes tabs** (toggle): the mouse's Forward side button sends Ctrl+W in browsers, editors and File Explorer, Ctrl+F4 in SSMS, Visual Studio and WebStorm, and Alt+F4 in every other app. In TeamViewer and Synergy the button is left alone. The per-app table is in `src\TrayTweaks\Tweaks\ForwardButtonCloseTweak.cs`. Works with any mouse that has a Forward button.
- **Back button combines Explorer windows** (toggle): the mouse's Back side button runs Combine stacked Explorer windows, whatever app is in front. In Excel it instead selects everything, autofits every row and column, and goes to the first cell. In a window with "Connect" in its title it fills in a login: Tab, the text saved on the Passwords tab, then Enter three times (with nothing saved, the button is left alone there). Works with any mouse that has a Back button.
- **Keep Outlook running** (toggle): checks every 30 seconds and starts classic Outlook when it isn't running. Outlook is started without administrator rights, through Explorer, even though TrayTweaks itself runs as administrator. Refuses to turn on where Outlook isn't installed.

## Mouse reference

The Mouse tab in Settings shows what each button on my Logitech G604 sends, on its own and with G-Shift held. It is a snapshot kept in `src\TrayTweaks\Mouse\MouseMap.cs`; remapping the mouse does not update it.

## Adding a tweak

1. Add a class in `src\TrayTweaks\Tweaks` that inherits `ToggleTweak` (stays on or off; implement `Enable()` and `Disable()`) or `ActionTweak` (runs once per click; implement `Run()`), plus `Id` and `Name`. `Description` and `Glyph` (its icon) are optional. `KeepOutlookRunningTweak.cs` and `CombineExplorerWindowsTweak.cs` are the examples. A tweak that reacts to the Back or Forward mouse button can use `Mouse\MouseHook.cs`, and `Input\Keyboard.cs` sends key presses. Anything a tweak launches would run as administrator; use `Input\UnelevatedLauncher.cs` to start a program at the normal level instead.
2. Add it to `AllTweaks` in `TweakManager.cs`.

It then shows up in the tray menu and Settings. A toggle's on/off state is saved.

## Settings

- On/off state: `%AppData%\TrayTweaks\settings.json`
- Passwords: text the app types for you is entered on the Passwords tab and kept in `%LocalAppData%\TrayTweaks\secrets.json`, each entry encrypted with Windows DPAPI for the current user. It is never in the source or in `settings.json`, and the file is useless on another PC or account. To add one, define a `SecretSlot` and list it in `SecretStore.Slots`.
- The Settings window's background is blurred glass on Windows 11 22H2 or later. The blur relies on an undocumented Windows call, so a Windows update could turn it back into a plain background.
- Start at sign-in: a Task Scheduler task named `TrayTweaks` (at sign-in of the current user, run with highest privileges). A Run-key entry doesn't work here, because Windows skips sign-in programs that need elevation. The task points at the exe that was running when the box was ticked; untick and tick again after moving the app.

## Installer

Needs [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`).

```powershell
.\installer\build-installer.ps1
```

This publishes a self-contained build and writes `installer\output\TrayTweaks-Setup-<version>.exe`. It installs per user (no admin prompt) to `%LocalAppData%\Programs\TrayTweaks`. Start at sign-in is not an installer option; tick it in Settings after installing.

`app.ico` is the favicon from jesse-hufstetler.github.io.
