namespace JournalApp.Services;

/// <summary>
/// Small key/value settings persisted via MAUI Preferences.
/// </summary>
/// <remarks>
/// Each value is read from Preferences once and then served from memory. On iOS every
/// Preferences call disposes the <c>NSUserDefaults.StandardUserDefaults</c> singleton it borrowed,
/// so a later read through a stale wrapper can come back null even though the value is stored —
/// which made two reads of the same setting disagree and broke the Notion import.
/// </remarks>
public static class AppSettings
{
    private static string? _NotionParentPageId;
    private static string? _NotionDatabaseId;
    private static string? _NotionDataSourceId;
    private static bool? _NotionSchemaReady;
    private static DateTime? _LastNotionUploadAt;
    private static bool? _SetupCompleted;
    private static bool? _PinSet;
    private static string? _UserName;

    /// <summary>ID of the workspace-level page that hosts the "Journal" database, cached after it is first created.</summary>
    public static string? NotionParentPageId
    {
        get => _NotionParentPageId ??= Preferences.Get(nameof(NotionParentPageId), string.Empty);
        set
        {
            _NotionParentPageId = value ?? string.Empty;
            Preferences.Set(nameof(NotionParentPageId), _NotionParentPageId);
        }
    }

    /// <summary>ID of the Notion "Journal" database, cached after it is first created/found.</summary>
    public static string? NotionDatabaseId
    {
        get => _NotionDatabaseId ??= Preferences.Get(nameof(NotionDatabaseId), string.Empty);
        set
        {
            _NotionDatabaseId = value ?? string.Empty;
            Preferences.Set(nameof(NotionDatabaseId), _NotionDatabaseId);
        }
    }

    /// <summary>ID of the database's data source, used as the parent when creating rows.</summary>
    public static string? NotionDataSourceId
    {
        get => _NotionDataSourceId ??= Preferences.Get(nameof(NotionDataSourceId), string.Empty);
        set
        {
            _NotionDataSourceId = value ?? string.Empty;
            Preferences.Set(nameof(NotionDataSourceId), _NotionDataSourceId);
        }
    }

    /// <summary>Set once the Notion data source is known to carry every property the app uploads,
    /// so the one-off schema patch is not re-sent on every upload.</summary>
    public static bool NotionSchemaReady
    {
        get => _NotionSchemaReady ??= Preferences.Get(nameof(NotionSchemaReady), false);
        set
        {
            _NotionSchemaReady = value;
            Preferences.Set(nameof(NotionSchemaReady), value);
        }
    }

    /// <summary>When the last successful Notion upload finished; default when there has never been one.</summary>
    public static DateTime LastNotionUploadAt
    {
        get => _LastNotionUploadAt ??= Preferences.Get(nameof(LastNotionUploadAt), default(DateTime));
        set
        {
            _LastNotionUploadAt = value;
            Preferences.Set(nameof(LastNotionUploadAt), value);
        }
    }

    /// <summary>Set once the user has finished (or skipped through) the onboarding wizard.</summary>
    public static bool SetupCompleted
    {
        get => _SetupCompleted ??= Preferences.Get(nameof(SetupCompleted), false);
        set
        {
            _SetupCompleted = value;
            Preferences.Set(nameof(SetupCompleted), value);
        }
    }

    /// <summary>Whether an app PIN exists. Mirrors the keystore entry written by
    /// <see cref="SecureSettings.SetPinAsync"/> so the shell can pick its start page synchronously.</summary>
    public static bool PinSet
    {
        get => _PinSet ??= Preferences.Get(nameof(PinSet), false);
        set
        {
            _PinSet = value;
            Preferences.Set(nameof(PinSet), value);
        }
    }

    /// <summary>Name the user gave during setup, used to greet them on the Today screen.</summary>
    public static string UserName
    {
        get => _UserName ??= Preferences.Get(nameof(UserName), string.Empty);
        set
        {
            _UserName = value ?? string.Empty;
            Preferences.Set(nameof(UserName), _UserName);
        }
    }
}
