# Publishes the app and compiles the Inno Setup installer into installer\output.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src\TrayTweaks\TrayTweaks.csproj'
$publish = Join-Path $root 'publish'

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish $project -c Release -r win-x64 --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 was not found. Install it with: winget install JRSoftware.InnoSetup' }

$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version
& $iscc "/DAppVersion=$version" (Join-Path $PSScriptRoot 'TrayTweaks.iss')
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compile failed' }
