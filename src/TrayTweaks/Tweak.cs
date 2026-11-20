namespace TrayTweaks;

/// <summary>
/// One thing the app can turn on and off. To add a tweak: subclass this in the
/// Tweaks folder and add it to <see cref="TweakManager.AllTweaks"/>.
/// </summary>
public abstract class Tweak
{
    /// <summary>Stable key saved in settings.json. Don't change it once released.</summary>
    public abstract string Id { get; }
    public abstract string Name { get; }
    public virtual string Description => "";

    // Both run on the UI thread, at startup/exit and when toggled.
    public abstract void Enable();
    public abstract void Disable();
}
