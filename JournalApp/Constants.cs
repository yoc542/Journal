namespace JournalApp;

/// <summary>App-wide constants.</summary>
public static class Constants
{
    public const int MaxJournalLength = 10_000;

    // Loads a .env file (searching this directory and its ancestors) into the process
    // environment before NotionToken is read below. Declared first so its field
    // initializer runs before NotionToken's, per C# static field init ordering.
    private static readonly bool _DotEnvLoaded = LoadDotEnv();

    /// <summary>
    /// Desktop-dev fallback for the Notion integration token, read from the NOTIONTOKEN environment
    /// variable (a real OS variable, or a local .env file loaded via DotNetEnv). On Android/iOS there
    /// is no .env file to find, so the token comes from
    /// <see cref="Services.SecureSettings.GetNotionTokenAsync"/> instead — see the settings page.
    /// </summary>
    public static readonly string NotionToken = Environment.GetEnvironmentVariable("NOTIONTOKEN") ?? string.Empty;

    private static bool LoadDotEnv()
    {
        try
        {
            DotNetEnv.Env.TraversePath().Load();
        }
        catch { /* no .env file found; fall back to a real environment variable, if any */ }
        return true;
    }

    public const string NotionVersion = "2026-03-11";

    /// <summary>Toggle to reveal developer-only features, e.g. importing entries back from Notion.</summary>
    public static bool DeveloperMode { get; set; } = true;
}
