namespace TrayTweaks.Tweaks;

public sealed class CombineExplorerWindowsTweak : ActionTweak
{
    public override string Id => "combine-explorer-windows";
    public override string Name => "Combine stacked Explorer windows";
    public override string Description => "Merges File Explorer windows that sit on exactly the same spot into one window with tabs.";
    public override string Glyph => "";

    public override string Run()
    {
        var result = ExplorerTabCombiner.CombineStacked();
        if (!result.TabsSupported)
            return "File Explorer tabs need Windows 11 (22H2 or later).";

        var skipped = result.WindowsSkipped == 0
            ? ""
            : $" {Count(result.WindowsSkipped, "window")} left alone because a tab could not be reopened.";
        return result.WindowsClosed == 0
            ? "No File Explorer windows are stacked on the same spot." + skipped
            : $"Moved {Count(result.TabsMoved, "tab")} and closed {Count(result.WindowsClosed, "window")}." + skipped;
    }

    static string Count(int n, string noun) => n == 1 ? $"1 {noun}" : $"{n} {noun}s";
}
