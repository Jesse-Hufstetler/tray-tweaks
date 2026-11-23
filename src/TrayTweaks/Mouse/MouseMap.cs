namespace TrayTweaks.Mouse;

/// <param name="Label">What to call the button: the label printed on it, or where it is.</param>
/// <param name="Sends">What Windows receives when it is pressed.</param>
/// <param name="Shifted">What Windows receives when it is pressed with G-Shift held; empty for the G-Shift button itself.</param>
public sealed record MouseButton(int Number, string Label, string Sends, string Shifted)
{
    public string ShiftedLine => Shifted.Length == 0 ? "hold for second layer" : $"G-Shift: {Shifted}";
}

/// <summary>
/// Jesse's Logitech G604, profile "Hybrid MF", as read from the mouse's onboard memory on 2026-11-20.
/// A snapshot for reference: remapping the mouse does not update it.
/// </summary>
public static class G604Map
{
    /// <summary>The 2 x 3 thumb grid as seen from the left side, front of the mouse first, top row then bottom row.</summary>
    public static readonly IReadOnlyList<MouseButton> ThumbGrid =
    [
        new(9, "G9", "Ctrl + W", "F20"),
        new(8, "G8", "Alt + F4", "F19"),
        new(7, "G7", "F14", "F18"),
        new(6, "G6", "Forward", "Back"),
        new(5, "G5", "G-Shift", ""),
        new(4, "G4", "F13", "F17"),
    ];

    public static readonly IReadOnlyList<MouseButton> OtherButtons =
    [
        new(1, "Left click", "Left click", "Ctrl + ="),
        new(2, "Right click", "Right click", "Ctrl + -"),
        new(3, "Wheel click", "Middle click", "Media key"),
        new(11, "+ button", "F16", "F22"),
        new(10, "- button", "F15", "F21"),
        new(12, "Wheel tilt left", "Tilt left", "F23"),
        new(13, "Wheel tilt right", "Tilt right", "F24"),
        new(15, "Scroll up", "Scroll up", "Media key"),
        new(14, "Scroll down", "Scroll down", "Media key"),
    ];
}
