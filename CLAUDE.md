# TrayTweaks: notes for Claude sessions

Jesse's personal Windows tray utility (WPF, .NET 10) that hosts small Windows tweaks. The README covers what it does, how to build it and how to add a tweak. This file holds what the README doesn't: working rules and the list of open items. Keep the open items current: remove one when it is done, add one when something is left unfinished or unverified.

## Working rules

- Personal project, not tied to a Jira ticket: commit subjects have no ticket key. Work on `main`, no branches or PRs.
- The repo is public. The repo-local `user.email` is Jesse's GitHub noreply address; leave it that way so a work email never lands in a public commit.
- The app's manifest is `requireAdministrator`, on purpose: Jesse is moving his AutoHotkey scripts into it and those need admin. Start it from an elevated shell (a normal `dotnet run` fails with error 740). Anything the app launches is elevated too.
- Jesse's AutoHotkey scripts (repo `autohotkey-script`: `Jesse.ahk`, `Automater.ahk`) are being replaced piece by piece: he asks for a feature when he notices he misses it. Don't port them wholesale. They contain passwords: never copy a password, or any script text that includes one, into this public repo, a commit message or a chat reply. Password-typing macros are planned, with the text entered in the app and stored only on the device (encrypt with DPAPI, never in source or in a synced file).
- Exit the running TrayTweaks (tray menu > Exit) before rebuilding, or the build can't overwrite the exe. If you end the process instead, a ghost tray icon stays until the mouse passes over it.
- To check how a window looks without using Jesse's screen: a throwaway WPF console project that references the app project, creates a plain `Application` with `ThemeMode` set (not `TrayTweaks.App`, whose startup would create a tray icon), shows the window off-screen, renders `window.Content` with `RenderTargetBitmap` to a PNG, and reads the PNG. Stop the running app first, because building the reference rebuilds its exe.
- Icons are glyphs that must exist in both Segoe Fluent Icons (Windows 11) and Segoe MDL2 Assets (Windows 10). Check both font files before using a new one.
- Tray menu items are deliberately not `IsCheckable`: the Fluent theme gives checkable items their own column, which pushes the other items' icons out of place. The checkmark goes in the icon slot.
- Never run `ExplorerTabCombiner.CombineStacked()` unfiltered while testing; it would rearrange Jesse's real Explorer windows. Open throwaway Explorer windows and pass the `include` filter so only those are touched, and close them afterwards. The earlier test harness was a throwaway console project that linked `ExplorerTabCombiner.cs`, opened four windows on temp folders, stacked three with `SetWindowPos`, combined, and checked the result through `ListTabs()`.

## What was learned about Explorer tabs (Windows 11 build 26300)

- New tab in a specific window: post `WM_COMMAND` `0xA21B` to one of the window's `ShellTabWindowClass` children. Undocumented, so recheck after big Windows updates.
- Navigating a new tab before it finishes loading its default folder gets lost or lands in the wrong tab. `OpenTab` waits for the tab to be idle, navigates, then verifies.
- `Navigate2` rejects a bare `::{guid}` path (This PC, Home); it needs `shell:` in front.
- `WM_CLOSE` on a multi-tab Explorer window closes only the active tab, so `CloseWindow` repeats it.

## Mouse buttons (Logitech G604)

- The G604 is in onboard-memory mode, so the button mapping lives in the mouse. G HUB's `settings.db` only holds placeholder software profiles and does not show it. The true map has to come from G HUB's Onboard Memory Manager screen or from pressing each button.
- The mouse has 16 assignable controls and two layers (default, and G-Shift held). The only profile Jesse uses is "Hybrid MF". Its map, read from Logitech Onboard Memory Manager screenshots on 2026-11-20:

  | Button | Default | With G-Shift held |
  |---|---|---|
  | 1 | Left click | Ctrl+= |
  | 2 | Right click | Ctrl+- |
  | 3 | Middle click | Media key (which one not recorded) |
  | 4 | F13 | F17 |
  | 5 | G-Shift | G-Shift |
  | 6 | Forward (Mouse 5, `XButton2`) | Back (Mouse 4, `XButton1`) |
  | 7 | F14 | F18 |
  | 8 | Alt+F4 | F19 |
  | 9 | Ctrl+W | F20 |
  | 10 | F15 | F21 |
  | 11 | F16 | F22 |
  | 12 | Tilt Left | F23 |
  | 13 | Tilt Right | F24 |
  | 14 | Scroll Down | Media key (which one not recorded) |
  | 15 | Scroll Up | Media key (which one not recorded) |
  | 16 | No Action | No Action |

- Physical positions. Buttons 4 to 9 are the six thumb buttons, which have G4 to G9 printed on them; the number matches the label. Seen from the left side with the front of the mouse to the left, the top row is G9, G8, G7 and the bottom row is G6, G5, G4. So button 5 (G-Shift) is bottom centre, which is what Jesse said, and button 6 (Forward by factory default and in his profile) is bottom front. Buttons 1 to 3 are left, right and wheel click; 12 to 15 are the wheel tilts and scroll directions. Beside the left click, button 10 is the - button and button 11 is the + button (Jesse checked with Identify Button). Button 16 is unidentified (No Action on both layers). The number-to-label match is inferred from those two agreeing points and a review photo, not from Logitech documentation.
- He rarely remaps, but can, so treat this table as a snapshot. The same data is in `src/TrayTweaks/Mouse/MouseMap.cs`, which feeds the Mouse tab in Settings; keep the two in step.
- The AutoHotkey scripts have a hotkey for every one of F13 to F24 and for `XButton1` / `XButton2`. Those two are also what his smaller mice send from their side buttons, so mouse features must keep working with a plain two-side-button mouse.
- To list the scripts' hotkeys without exposing what they type, print only the trigger and the command word of each action, never the arguments.

## Open items

Not verified yet:

- The Combine stacked Explorer windows action has only been tested through the harness. Nobody has confirmed the in-app path: tray menu click or Settings Run button, then the result notification.
- In Settings, nobody has confirmed that the checkboxes stay in sync when a tweak is toggled from the tray menu. (The layout of both tabs was checked in light and dark mode on 2026-11-20 by off-screen render.)
- Nothing has been run on Windows 10: the Fluent theme's look, the MDL2 icon fallback, and the "needs Windows 11" message from the combine action.
- Nothing has been checked on a scaled (above 100%) display.
- Start at sign-in now that the app requires admin: it still uses the HKCU Run key, and Windows can silently skip Run-key programs that need elevation. Jesse's UAC is set to elevate without prompting, which may let it through. Unverified until he signs out and back in. If it doesn't start, the usual fix is a Task Scheduler logon task with highest privileges; flag the antivirus angle to him before building that (an unsigned app creating an elevated logon task).
- Combining a window that has a search-results tab is untested. It should be left alone and reported as skipped.

Not built yet:

- The installer has never been compiled. Inno Setup 6 was not installed, so `installer\build-installer.ps1` and `TrayTweaks.iss` are untested.
- Auto-update: Jesse wants the app to eventually check GitHub Releases and install a newer version of itself. He said to discuss it before building it.
- No test project in the repo. The combiner harness was throwaway; a kept version would be worth adding.

State of the AutoHotkey hand-over (machine state, not in this repo):

- Both scripts were started at sign-in by `init.bat` through a shortcut in his Startup folder. That shortcut was disabled on 2026-11-20 the Task Manager way (Startup apps > `init.bat - Shortcut.lnk` > Disabled), so he can re-enable it there. The scripts were left running for the current session.
- Nothing from the scripts has been ported yet.

Decisions waiting on Jesse:

- Keep awake was added as an example tweak. He has not said whether to keep it.
- The name TrayTweaks is a working name.
