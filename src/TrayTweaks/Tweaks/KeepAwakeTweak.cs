using System.Runtime.InteropServices;

namespace TrayTweaks.Tweaks;

/// <summary>Example tweak: stops Windows from sleeping or turning the screen off.</summary>
public sealed class KeepAwakeTweak : ToggleTweak
{
    const uint ES_CONTINUOUS = 0x80000000;
    const uint ES_SYSTEM_REQUIRED = 0x00000001;
    const uint ES_DISPLAY_REQUIRED = 0x00000002;

    [DllImport("kernel32.dll")]
    static extern uint SetThreadExecutionState(uint esFlags);

    public override string Id => "keep-awake";
    public override string Name => "Keep awake";
    public override string Description => "Stops Windows from sleeping or turning off the screen while this is on.";
    public override string Glyph => "";

    // The state belongs to the calling thread, so it lasts only while the UI thread lives.
    public override void Enable() => SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
    public override void Disable() => SetThreadExecutionState(ES_CONTINUOUS);
}
