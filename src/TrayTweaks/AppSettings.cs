using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrayTweaks;

public sealed class AppSettings
{
    /// <summary>
    /// Only the tweaks the user has switched on or off themselves. Every other tweak follows its
    /// <see cref="ToggleTweak.OnByDefault"/>, so a tweak added later starts out on.
    /// </summary>
    public Dictionary<string, bool> Tweaks { get; set; } = [];

    /// <summary>
    /// The format up to 0.1.0, when tweaks were off unless listed here. Read once and folded into
    /// <see cref="Tweaks"/>; never written again.
    /// </summary>
    public HashSet<string>? EnabledTweaks { get; set; }

    public bool IsEnabled(ToggleTweak tweak) => Tweaks.TryGetValue(tweak.Id, out var on) ? on : tweak.OnByDefault;

    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrayTweaks", "settings.json");

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return FromJson(File.ReadAllText(FilePath));
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            AppLog.Write($"The settings file could not be loaded: {ex.GetType().Name} {ex.Message.Trim()}");
        }
        return new();
    }

    public static AppSettings FromJson(string json)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new();
        foreach (var id in settings.EnabledTweaks ?? [])
            settings.Tweaks.TryAdd(id, true);
        settings.EnabledTweaks = null;
        return settings;
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, ToJson());
    }
}
