namespace JournalApp;

/// <summary>App-wide constants.</summary>
public static class Constants
{
    public const int MaxJournalLength = 10_000;

    public const int MaxUserNameLength = 20;

    public const int PinLength = 4;

    public const string PinBackspaceKey = "back";

    public const int MaxIntentionTitleLength = 60;

    public const int MaxIntentionNoteLength = 120;

    public const int MaxIntentionLogLength = 1_000;

    public const int IntentionUidLength = 8;

    public const string NotionVersion = "2026-03-11";

}

public static class Routes
{
    public const string Lock = "LockPage";

    public const string Onboarding = "OnboardingPage";

    public const string Today = "TodayPage";

    public const string JournalEditor = "JournalEditorPage";

    public const string JournalList = "JournalListPage";

    public const string EntryDetail = "EntryDetailPage";

    public const string Intentions = "IntentionsPage";

    public const string NotionConnect = "NotionConnectPage";

    public const string Upload = "UploadPage";

    public const string Import = "ImportPage";

    public const string Settings = "SettingsPage";

    public const string Pin = "PinPage";
}
