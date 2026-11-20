# TrayTweaks

A small Windows system tray utility that holds my personal Windows tweaks and automations. Each tweak can be run or switched on and off from the tray menu or the Settings window, and the app can start when I sign in. Runs on Windows 10 and 11.

## Build and run

Needs the .NET 10 SDK.

```powershell
dotnet run --project src\TrayTweaks
```

Right-click the tray icon to toggle tweaks, open Settings or exit. Double-click opens Settings.

## Tweaks

- **Combine stacked Explorer windows** (action, Windows 11 22H2 or later): merges File Explorer windows that sit on exactly the same spot, for example in the same FancyZones zone, into one window with tabs. Explorer has no API for moving a tab, so each tab is reopened in the front window at the same folder and the emptied window is closed; a tab's back history and selection are not carried over. A window is only closed once all of its tabs are confirmed open in the front window.
- **Keep awake** (toggle): stops Windows from sleeping or turning off the screen.

## Adding a tweak

1. Add a class in `src\TrayTweaks\Tweaks` that inherits `ToggleTweak` (stays on or off; implement `Enable()` and `Disable()`) or `ActionTweak` (runs once per click; implement `Run()`), plus `Id` and `Name`. `Description` and `Glyph` (its icon) are optional. `KeepAwakeTweak.cs` and `CombineExplorerWindowsTweak.cs` are the examples.
2. Add it to `AllTweaks` in `TweakManager.cs`.

It then shows up in the tray menu and Settings. A toggle's on/off state is saved.

## Settings

- On/off state: `%AppData%\TrayTweaks\settings.json`
- Start with Windows: the `TrayTweaks` value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

## Installer

Needs [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`).

```powershell
.\installer\build-installer.ps1
```

This publishes a self-contained build and writes `installer\output\TrayTweaks-Setup-<version>.exe`. It installs per user (no admin prompt) to `%LocalAppData%\Programs\TrayTweaks`.

`app.ico` is the favicon from jesse-hufstetler.github.io.
