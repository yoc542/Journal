namespace JournalApp.Services;

/// <summary>Secrets persisted in the platform keystore/keychain via MAUI SecureStorage.</summary>
public static class SecureSettings
{
    private const string NotionTokenKey = "NotionToken";
    private const string AppPinKey = "AppPin";

    /// <summary>The Notion integration token entered on the settings page.</summary>
    public static async Task<string> GetNotionTokenAsync()
    {
        string? stored = null;
        try { stored = await SecureStorage.GetAsync(NotionTokenKey); }
        catch { /* keystore unavailable or entry undecryptable — treat as unset */ }

        return stored ?? string.Empty;
    }

    public static Task SetNotionTokenAsync(string token) => SecureStorage.SetAsync(NotionTokenKey, token);

    public static void ClearNotionToken() => SecureStorage.Remove(NotionTokenKey);

    public static async Task<string> GetPinAsync()
    {
        string? stored = null;
        try { stored = await SecureStorage.GetAsync(AppPinKey); }
        catch { }

        return stored ?? string.Empty;
    }

    public static async Task SetPinAsync(string pin)
    {
        await SecureStorage.SetAsync(AppPinKey, pin);
        AppSettings.PinSet = true;
    }
}
