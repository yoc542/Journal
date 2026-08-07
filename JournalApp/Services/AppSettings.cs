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
}
