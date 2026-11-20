# TrayTweaks

A small Windows system tray utility that holds my personal Windows tweaks and automations. Each tweak can be turned on or off from the tray menu or the Settings window, and the app can start when I sign in.

## Build and run

Needs the .NET 10 SDK.

```powershell
dotnet run --project src\TrayTweaks
```

Right-click the tray icon to toggle tweaks, open Settings or exit. Double-click opens Settings.

## Adding a tweak

1. Add a class in `src\TrayTweaks\Tweaks` that inherits `Tweak` and implements `Id`, `Name`, `Enable()` and `Disable()`. `KeepAwakeTweak.cs` is the example.
2. Add it to `AllTweaks` in `TweakManager.cs`.

It then shows up in the tray menu and Settings, and its on/off state is saved.

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
