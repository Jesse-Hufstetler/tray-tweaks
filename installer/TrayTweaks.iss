; Per-user installer (no admin prompt). Build with installer\build-installer.ps1.
#define AppName "TrayTweaks"
#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId={{6B0E3B0B-6C1E-4E57-9B0A-3C6C3B0D7A11}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=output
OutputBaseFilename={#AppName}-Setup-{#AppVersion}
SetupIconFile=..\src\TrayTweaks\app.ico
UninstallDisplayIcon={app}\TrayTweaks.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes

; No "start at sign-in" option here: the app needs admin, so that is a scheduled task the app
; creates itself from Settings (this installer runs without admin and cannot).

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\TrayTweaks.exe"

[Registry]
; Versions up to 0.1.0 used a Run value for start at sign-in; clean it up on uninstall.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "TrayTweaks"; Flags: uninsdeletevalue dontcreatekey

[Run]
Filename: "{app}\TrayTweaks.exe"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent shellexec
