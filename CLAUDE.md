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

- The Combine stacked Explorer windows action was tested through the harness; Jesse has since used it in the app. On 2026-11-21 he asked for its notification to go, so it is now silent when it combines windows or finds nothing to combine. It still notifies when a window was left alone because a tab could not be reopened, and on Windows 10 (that was Claude's choice of what counts as worth a notification, not something he spelled out).
- In Settings, nobody has confirmed that the checkboxes stay in sync when a tweak is toggled from the tray menu. (The layout of both tabs was checked in light and dark mode on 2026-11-20 by off-screen render.)
- The Settings window is untinted blurred glass (`WindowBackdrop.cs`, 2026-11-21). Jesse wanted far more see-through than Mica or Windows' acrylic, in the direction of Aero glass; he called this version "pretty awesome". He tried a tint slider, liked it all the way up, and asked for it to be fixed there with no option, so there is no setting. Because nothing tints the glass, it can come out light or dark (over a white window it is mid grey), and the XAML allows for that: text straight on the glass has a halo in the theme's window colour (`GlassGlow`), and panels use `PlateBrush` (that colour at 70%) instead of the theme's card brush, which is nearly transparent in dark mode. Keep both when adding UI to this window. It took five tries; what was learned, so nobody repeats them:
  - The DWM acrylic backdrop (`DWMSBT_TRANSIENTWINDOW`) can't be adjusted and turns flat grey when the window loses focus.
  - The undocumented `SetWindowCompositionAttribute` accent works, focused or not, but only after the system backdrop is set to none and `DwmExtendFrameIntoClientArea` is called with zero margins. The Fluent theme extends the frame over the whole window, and that frame hides the accent.
  - Accent state 4 (acrylic blur) adds its own milky layer, so its tint alpha changes almost nothing and it washes out to flat white over a light window. Jesse found it less glassy than DWM acrylic.
  - Accent state 2 (tint, no blur) follows alpha exactly, but he did not like sharp content showing through.
  - Accent state 3 (blur) with `AccentFlags = 0` is the one in use: strong blur, no tint, colour ignored. With the usual flags value 2 it renders black on this build.
  - While focused, the title bar is solid in his accent colour; unfocused it is plain.
  - Checked on the test card in light and dark theme, both tabs, focused. Not checked: dragging the window, a theme switch while it is open, and Windows 10 (where the glass is skipped and the window should be solid; Jesse said "at some point, we'll go see how it looks on Windows 10").
- To look at a real window backdrop without seeing Jesse's screen: a throwaway project shows an opaque, topmost striped "test card" window, puts the window under test on top of it (also topmost, `ShowActivated = false` so it takes no focus), and saves `Graphics.CopyFromScreen` of the card's rectangle only. Nothing of his is in the picture. Tell him first that test windows will flash at the top-left for a few seconds.
- Jesse likes dense, classic-Windows layouts and dislikes roomy modern ones. The Mouse tab was rebuilt that way on 2026-11-21 (plain bordered tables, 12 px text; checked by off-screen render in light and dark). The Tweaks tab still has the roomy card layout; he has not asked for it to change.
- Nothing has been run on Windows 10: the Fluent theme's look, the MDL2 icon fallback, and the "needs Windows 11" message from the combine action.
- Nothing has been checked on a scaled (above 100%) display.
- Start at sign-in was rebuilt on 2026-11-21 as a Task Scheduler logon task (`StartupRegistration.cs`, task name `TrayTweaks`, highest privileges), after the HKCU Run key proved not to work: on that day's restart Windows tried the Run entry and started nothing (Shell-Core event log: "Finished execution of command 'TrayTweaks.exe' (PID 0)"), because the app needs elevation. Jesse approved the scheduled task. Jesse ticked the Settings checkbox that day and the task was read back afterwards: it exists, runs with highest privileges at his sign-in and points at the Debug build's exe, and the old Run value is gone. Still to confirm: the app actually starts at his next sign-in. Don't register test tasks from a throwaway exe: an unsigned exe in Temp creating an elevated logon task is exactly what antivirus watches for.
- Combining a window that has a search-results tab is untested. It should be left alone and reported as skipped.
- Forward button closes tabs (added 2026-11-20): only the app-to-action table and the hook installing were checked, by a throwaway console project that sends nothing. Nobody has pressed the real button with the tweak on. Never test it by injecting a Forward press or by sending the keys: it would close whatever Jesse has in front. While `Jesse.ahk` is still running it has the same hotkey; TrayTweaks' hook is installed later, so it should answer first, but that is unconfirmed.

- Keep Outlook running (added 2026-11-21): the un-elevated launch was checked (an elevated throwaway exe relaunched itself through `UnelevatedLauncher` and the child had no admin rights), and so were finding Outlook's path and seeing that it is running. Not checked: the tweak actually restarting Outlook after it is closed, because that means closing Jesse's Outlook. It is for his work PC only, so it stays off by default.

Not built yet:

- The installer has never been compiled. Inno Setup 6 was not installed, so `installer\build-installer.ps1` and `TrayTweaks.iss` are untested. Uninstalling does not remove the start-at-sign-in task (the uninstaller has no admin rights); untick the box in Settings first, or add a step for it.
- Auto-update: Jesse wants the app to eventually check GitHub Releases and install a newer version of itself. He said to discuss it before building it.
- No test project in the repo. The combiner harness was throwaway; a kept version would be worth adding.

State of the AutoHotkey hand-over (machine state, not in this repo):

- Both scripts were started at sign-in by `init.bat` through a shortcut in his Startup folder. Disabling that shortcut the Task Manager way (the `StartupApproved\StartupFolder` registry flag, set 2026-11-20) did not work: on 2026-11-21 the scripts started at sign-in anyway, in the same second as the other Startup-folder items, with the flag still reading disabled. Why Windows ignored it is unknown. So on 2026-11-21 the shortcut `init.bat - Shortcut.lnk` was moved out of the Startup folder to sit beside `init.bat` in his work OneDrive folder; to bring the scripts back at sign-in, move it back into `shell:startup`. Unconfirmed until his next sign-in. If the scripts still start, ask him to leave them running so the parent process can be read.
- Ported so far: the Forward button (`XButton2`) hotkey, as the Forward button closes tabs tweak. Its per-app table was copied row for row, including rows that may be dead (QuickBooks POS, VbsEdit); Jesse has not said which to drop.
- Also ported: the script's Outlook check (it started Outlook from inside its Ctrl+S hotkey, with `runas /trustlevel`), as the Keep Outlook running tweak. Jesse called the Ctrl+S trigger janky and did not want it copied, so the tweak polls on a timer.
- Not ported: the Back button (`XButton1`) hotkey. In the script it types a login into a window titled "Connect" (Jesse thinks of this as the Nortridge admin password fill, and remembered it as being on Forward), autofits columns in Excel, and otherwise sends Win+D. The login part waits for the password-macro design (text entered in Settings, DPAPI).

Decisions waiting on Jesse:

- The name TrayTweaks is a working name.

Removed, may come back:

- The Keep awake example tweak was removed on 2026-11-21: Jesse already has other apps for that. To bring it back, restore `src/TrayTweaks/Tweaks/KeepAwakeTweak.cs` from git history (last present in commit e400274) and add it to `AllTweaks`.
