namespace JournalApp.Services;

/// <summary>Small key/value settings persisted via MAUI Preferences.</summary>
public static class AppSettings
{
    /// <summary>ID of the workspace-level page that hosts the "Journal" database, cached after it is first created.</summary>
    public static string? NotionParentPageId
    {
        get => Preferences.Get(nameof(NotionParentPageId), null);
        set => Preferences.Set(nameof(NotionParentPageId), value ?? string.Empty);
    }

    /// <summary>ID of the Notion "Journal" database, cached after it is first created/found.</summary>
    public static string? NotionDatabaseId
    {
        get => Preferences.Get(nameof(NotionDatabaseId), null);
        set => Preferences.Set(nameof(NotionDatabaseId), value ?? string.Empty);
    }

    /// <summary>ID of the database's data source, used as the parent when creating rows.</summary>
    public static string? NotionDataSourceId
    {
        get => Preferences.Get(nameof(NotionDataSourceId), null);
        set => Preferences.Set(nameof(NotionDataSourceId), value ?? string.Empty);
    }

    /// <summary>Set once the Notion data source is known to have the "Entry Date" property,
    /// so the one-off schema patch is not re-sent on every upload.</summary>
    public static bool NotionEntryDateReady
    {
        get => Preferences.Get(nameof(NotionEntryDateReady), false);
        set => Preferences.Set(nameof(NotionEntryDateReady), value);
    }

    /// <summary>When the last successful Notion upload finished; default when there has never been one.</summary>
    public static DateTime LastNotionUploadAt
    {
        get => Preferences.Get(nameof(LastNotionUploadAt), default(DateTime));
        set => Preferences.Set(nameof(LastNotionUploadAt), value);
    }

    /// <summary>Set once the user has finished (or skipped through) the onboarding wizard.</summary>
    public static bool SetupCompleted
    {
        get => Preferences.Get(nameof(SetupCompleted), false);
        set => Preferences.Set(nameof(SetupCompleted), value);
    }

    /// <summary>Name the user gave during setup, used to greet them on the Today screen.</summary>
    public static string UserName
    {
        get => Preferences.Get(nameof(UserName), string.Empty);
        set => Preferences.Set(nameof(UserName), value ?? string.Empty);
    }
}
