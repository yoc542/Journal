using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Resources.Strings;
using JournalApp.Services;

namespace JournalApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly NotionService _Notion;

    [ObservableProperty] private string _Token = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _IsBusy;

    public SettingsViewModel(NotionService notion) => _Notion = notion;

    public bool IsNotBusy => !IsBusy;

    public string StatusLabel => string.IsNullOrWhiteSpace(Token)
        ? AppResources.Settings_Disconnected_Badge
        : AppResources.Settings_Connected_Badge;

    public async Task LoadAsync() => Token = await SecureSettings.GetNotionTokenAsync();

    partial void OnTokenChanged(string value) => OnPropertyChanged(nameof(StatusLabel));

    [RelayCommand]
    private async Task SaveAsync()
    {
        var token = (Token ?? string.Empty).Trim();
        if (token.Length == 0)
        {
            await Shell.Current.DisplayAlertAsync(
                AppResources.Settings_InvalidTitle, AppResources.Settings_EmptyMessage, AppResources.OK);
            return;
        }

        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            if (!await _Notion.IsTokenValidAsync(token))
            {
                await Shell.Current.DisplayAlertAsync(
                    AppResources.Settings_InvalidTitle, AppResources.Settings_InvalidMessage, AppResources.OK);
                return;
            }

            await SecureSettings.SetNotionTokenAsync(token);
            await _Notion.EnsureJournalDatabaseAsync();

            await Shell.Current.DisplayAlertAsync(
                AppResources.Settings_SavedTitle, AppResources.Settings_SavedMessage, AppResources.OK);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(AppResources.Settings_InvalidTitle, ex.Message, AppResources.OK);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        SecureSettings.ClearNotionToken();
        Token = string.Empty;
        await Shell.Current.DisplayAlertAsync(
            AppResources.Settings_ClearedTitle, AppResources.Settings_ClearedMessage, AppResources.OK);
    }
}
