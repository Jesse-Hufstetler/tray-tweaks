namespace TrayTweaks.Tweaks;

public sealed class CombineExplorerWindowsTweak : ActionTweak
{
    public override string Id => "combine-explorer-windows";
    public override string Name => "Combine stacked Explorer windows";
    public override string Description => "Merges File Explorer windows that sit on exactly the same spot into one window with tabs.";
    public override string Glyph => "";

    // Quiet when it worked or there was nothing to combine; only a problem gets a notification.
    public override string? Run()
    {
        var result = ExplorerTabCombiner.CombineStacked();
        if (!result.TabsSupported)
            return "File Explorer tabs need Windows 11 (22H2 or later).";

        return result.WindowsSkipped == 0
            ? null
            : $"{Count(result.WindowsSkipped, "window")} left alone because a tab could not be reopened.";
    }

    static string Count(int n, string noun) => n == 1 ? $"1 {noun}" : $"{n} {noun}s";
}
