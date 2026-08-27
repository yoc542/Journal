using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Localization;
using JournalApp.Services;

namespace JournalApp.ViewModels;

public partial class NotionConnectViewModel : ObservableObject
{
    private readonly NotionService _Notion;

    [ObservableProperty] private string _Token = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError), nameof(ButtonLabel))]
    private string _ErrorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _IsBusy;

    public NotionConnectViewModel(NotionService notion) => _Notion = notion;

    public bool HasError => ErrorMessage.Length > 0;
    public bool IsNotBusy => !IsBusy;

    /// <summary>The CTA becomes "Try again" once Notion has refused something.</summary>
    public string ButtonLabel => HasError
        ? AppResources.Notion_Connect_Retry
        : AppResources.Notion_Connect_Cta;

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (IsBusy)
            return;

        var token = (Token ?? string.Empty).Trim();

        if (token.Length == 0)
        {
            ErrorMessage = AppResources.Onboarding_Token_ErrorEmpty;
            return;
        }

        if (!token.StartsWith("ntn_", StringComparison.Ordinal) &&
            !token.StartsWith("secret_", StringComparison.Ordinal))
        {
            ErrorMessage = AppResources.Onboarding_Token_ErrorShape;
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            if (!await _Notion.ConnectAsync(token))
            {
                ErrorMessage = AppResources.Notion_Connect_Error;
                return;
            }

            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            ErrorMessage = AppResources.Notion_Connect_Error;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");
}
