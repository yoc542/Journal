using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Services;
using JournalApp.Views;

namespace JournalApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly JournalDatabase _Database;

    [ObservableProperty] private string _TokenMasked = string.Empty;
    [ObservableProperty] private string _SyncStatus = string.Empty;
    [ObservableProperty] private string _SyncLine = string.Empty;
    [ObservableProperty] private string _TokenActionLabel = string.Empty;
    [ObservableProperty] private string _FooterLabel = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNameNote))]
    private string _NameNote = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotConnected))]
    private bool _IsConnected;

    /// <summary>Name being edited; only written to settings when the user saves.</summary>
    [ObservableProperty] private string _UserName = string.Empty;

    public SettingsViewModel(JournalDatabase database) => _Database = database;

    public int MaxUserNameLength => Constants.MaxUserNameLength;

    public bool IsNotConnected => !IsConnected;

    public bool HasNameNote => NameNote.Length > 0;

    public async Task LoadAsync()
    {
        var token = await SecureSettings.GetNotionTokenAsync();
        IsConnected = !string.IsNullOrWhiteSpace(token);

        TokenMasked = IsConnected ? Mask(token) : AppResources.Settings_Token_NotSet;
        TokenActionLabel = IsConnected
            ? AppResources.Settings_Token_Change
            : AppResources.Settings_Token_Add;

        SyncStatus = IsConnected
            ? AppResources.Settings_Sync_Connected
            : AppResources.Settings_Sync_Disconnected;

        SyncLine = await BuildSyncLineAsync();

        UserName = AppSettings.UserName;
        NameNote = string.Empty;

        FooterLabel = string.Format(AppResources.Settings_Footer_Format, AppInfo.VersionString);
    }

    private async Task<string> BuildSyncLineAsync()
    {
        if (!IsConnected)
            return AppResources.Settings_Sync_Line_Disconnected;

        var lastUpload = AppSettings.LastNotionUploadAt;
        if (lastUpload == default)
            return AppResources.Settings_Sync_Line_Never;

        var uploaded = await _Database.GetEntryCountAsync() - await _Database.GetPendingUploadCountAsync();
        var when = lastUpload.ToString("g", CultureInfo.CurrentCulture);

        return uploaded == 1
            ? string.Format(AppResources.Settings_Sync_Line_One, when)
            : string.Format(AppResources.Settings_Sync_Line_Format, when, uploaded);
    }

    /// <summary>Shows enough of the token to recognise it without revealing it.</summary>
    private static string Mask(string token) =>
        token.Length <= 8
            ? "••••••••"
            : $"{token[..4]}•••••••••••••••• {token[^4..]}";

    /// <summary>Clears the "saved" note as soon as the field is edited again.</summary>
    partial void OnUserNameChanged(string value) => NameNote = string.Empty;

    [RelayCommand]
    private void SaveName()
    {
        UserName = (UserName ?? string.Empty).Trim();
        AppSettings.UserName = UserName;
        NameNote = AppResources.Settings_Name_Saved;
    }

    [RelayCommand]
    private static Task OpenUploadAsync() => Shell.Current.GoToAsync(nameof(UploadPage));

    [RelayCommand]
    private static Task OpenImportAsync() => Shell.Current.GoToAsync(nameof(ImportPage));

    [RelayCommand]
    private static Task OpenConnectAsync() => Shell.Current.GoToAsync(nameof(NotionConnectPage));

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");
}
