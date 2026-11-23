using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TrayTweaks.Tweaks;

namespace TrayTweaks.Secrets;

/// <summary>A piece of text the app types for the user, such as a password. Shown on the Passwords tab.</summary>
public sealed record SecretSlot(string Id, string Name, string Description);

/// <summary>
/// Keeps secrets in a file on this PC, each encrypted with Windows DPAPI for the current user, so the
/// file is useless on another PC or to another account. Any program running as the same user could
/// still ask Windows to decrypt it. Never put a secret in source, in settings.json or in a synced folder.
/// </summary>
public sealed class SecretStore(string filePath)
{
    /// <summary>Under Local (not Roaming) AppData, so it is neither synced nor roamed.</summary>
    public static readonly SecretStore Default = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrayTweaks", "secrets.json"));

    /// <summary>Every secret the app knows how to use. Add a slot here to get a row on the Passwords tab.</summary>
    public static readonly IReadOnlyList<SecretSlot> Slots =
        [BackButtonTweak.ConnectLogin, .. PasswordKeysTweak.Keys.Select(key => key.Slot)];

    static readonly byte[] Entropy = Encoding.UTF8.GetBytes("TrayTweaks.SecretStore");

    public bool Has(string id) => Load().ContainsKey(id);

    /// <summary>The stored text, or null when there is none or it can't be decrypted on this PC and account.</summary>
    public string? Get(string id)
    {
        if (!Load().TryGetValue(id, out var encrypted))
            return null;
        try
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(encrypted), Entropy, DataProtectionScope.CurrentUser));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return null;
        }
    }

    public void Set(string id, string value)
    {
        var all = Load();
        all[id] = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser));
        Save(all);
    }

    public void Remove(string id)
    {
        var all = Load();
        if (all.Remove(id))
            Save(all);
    }

    Dictionary<string, string> Load()
    {
        try
        {
            if (File.Exists(filePath))
                return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(filePath)) ?? [];
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
        }
        return [];
    }

    void Save(Dictionary<string, string> all)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, JsonSerializer.Serialize(all));
    }
}
