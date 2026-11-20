# TrayTweaks: notes for Claude sessions

Jesse's personal Windows tray utility (WPF, .NET 10) that hosts small Windows tweaks. The README covers what it does, how to build it and how to add a tweak. This file holds what the README doesn't: working rules and the list of open items. Keep the open items current: remove one when it is done, add one when something is left unfinished or unverified.

## Working rules

- Personal project, not tied to a Jira ticket: commit subjects have no ticket key. Work on `main`, no branches or PRs.
- The repo is public. The repo-local `user.email` is Jesse's GitHub noreply address; leave it that way so a work email never lands in a public commit.
- Exit the running TrayTweaks (tray menu > Exit) before rebuilding, or the build can't overwrite the exe. If you end the process instead, a ghost tray icon stays until the mouse passes over it.
- Icons are glyphs that must exist in both Segoe Fluent Icons (Windows 11) and Segoe MDL2 Assets (Windows 10). Check both font files before using a new one.
- Tray menu items are deliberately not `IsCheckable`: the Fluent theme gives checkable items their own column, which pushes the other items' icons out of place. The checkmark goes in the icon slot.
- Never run `ExplorerTabCombiner.CombineStacked()` unfiltered while testing; it would rearrange Jesse's real Explorer windows. Open throwaway Explorer windows and pass the `include` filter so only those are touched, and close them afterwards. The earlier test harness was a throwaway console project that linked `ExplorerTabCombiner.cs`, opened four windows on temp folders, stacked three with `SetWindowPos`, combined, and checked the result through `ListTabs()`.

## What was learned about Explorer tabs (Windows 11 build 26300)

- New tab in a specific window: post `WM_COMMAND` `0xA21B` to one of the window's `ShellTabWindowClass` children. Undocumented, so recheck after big Windows updates.
- Navigating a new tab before it finishes loading its default folder gets lost or lands in the wrong tab. `OpenTab` waits for the tab to be idle, navigates, then verifies.
- `Navigate2` rejects a bare `::{guid}` path (This PC, Home); it needs `shell:` in front.
- `WM_CLOSE` on a multi-tab Explorer window closes only the active tab, so `CloseWindow` repeats it.

## Open items

Not verified yet:

- The Combine stacked Explorer windows action has only been tested through the harness. Nobody has confirmed the in-app path: tray menu click or Settings Run button, then the result notification.
- The Settings window has not been looked at since the WPF rewrite (card backgrounds in light and dark mode, the Run button, checkbox sync with the tray menu).
- Nothing has been run on Windows 10: the Fluent theme's look, the MDL2 icon fallback, and the "needs Windows 11" message from the combine action.
- Nothing has been checked on a scaled (above 100%) display.
- Combining a window that has a search-results tab is untested. It should be left alone and reported as skipped.

Not built yet:

- The installer has never been compiled. Inno Setup 6 was not installed, so `installer\build-installer.ps1` and `TrayTweaks.iss` are untested.
- Auto-update: Jesse wants the app to eventually check GitHub Releases and install a newer version of itself. He said to discuss it before building it.
- No test project in the repo. The combiner harness was throwaway; a kept version would be worth adding.

Decisions waiting on Jesse:

- Keep awake was added as an example tweak. He has not said whether to keep it.
- The name TrayTweaks is a working name.
