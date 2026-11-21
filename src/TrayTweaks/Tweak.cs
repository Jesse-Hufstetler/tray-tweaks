namespace TrayTweaks;

/// <summary>
/// One thing the app can do. To add one: subclass <see cref="ToggleTweak"/> or <see cref="ActionTweak"/>
/// in the Tweaks folder and add it to <see cref="TweakManager.AllTweaks"/>.
/// </summary>
public abstract class Tweak
{
    /// <summary>Stable key saved in settings.json. Don't change it once released.</summary>
    public abstract string Id { get; }
    public abstract string Name { get; }
    public virtual string Description => "";

    /// <summary>Icon: a character that exists in both Segoe Fluent Icons and Segoe MDL2 Assets.</summary>
    public virtual string Glyph => "";
}

/// <summary>Something that stays on or off. Its state is saved and restored at startup.</summary>
public abstract class ToggleTweak : Tweak
{
    /// <summary>
    /// Runs an action tweak the way a click on it would (own thread, result notification).
    /// Set by <see cref="TweakManager"/>; call it on the UI thread.
    /// </summary>
    public Action<ActionTweak>? RunAction { get; set; }

    // Both run on the UI thread, at startup/exit and when toggled.
    public abstract void Enable();
    public abstract void Disable();
}

/// <summary>Something that runs once each time it is clicked.</summary>
public abstract class ActionTweak : Tweak
{
    /// <summary>Runs on its own STA thread. Returns a short result to show the user, or null for none.</summary>
    public abstract string? Run();
}
