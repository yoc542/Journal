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
    /// Notion integration token. Read from the NOTIONTOKEN environment variable, populated
    /// either by a real OS environment variable or by a local .env file loaded via DotNetEnv.
    /// Note: this only works on Windows, where the app can read files from its working directory tree.
    /// Android/iOS have no such .env file, so Notion features are simply unavailable there.
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

    public static bool NotionConfigured => !string.IsNullOrWhiteSpace(NotionToken);
}
