using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Localization;
using JournalApp.Services;
using JournalApp.Views;

namespace JournalApp.ViewModels;

/// <summary>Steps of the first-launch onboarding flow, shown one at a time on <see cref="OnboardingPage"/>.</summary>
public enum OnboardingStep
{
    Welcome,
    Profile,
    Token,
    Done,
}

public partial class OnboardingViewModel : ObservableObject
{
    private readonly NotionService _Notion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWelcome), nameof(IsProfile), nameof(IsToken), nameof(IsDone),
        nameof(StepLabel), nameof(IsSecondStep))]
    private OnboardingStep _Step = OnboardingStep.Welcome;

    [ObservableProperty] private string _Name = string.Empty;
    [ObservableProperty] private bool _ReminderEnabled = true;
    [ObservableProperty] private string _Token = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _ErrorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _IsBusy;

    public OnboardingViewModel(NotionService notion) => _Notion = notion;

    public bool IsWelcome => Step == OnboardingStep.Welcome;
    public bool IsProfile => Step == OnboardingStep.Profile;
    public bool IsToken => Step == OnboardingStep.Token;
    public bool IsDone => Step == OnboardingStep.Done;

    public bool IsSecondStep => Step == OnboardingStep.Token;
    public string StepLabel => string.Format(AppResources.Onboarding_Step_Format, IsSecondStep ? 2 : 1);

    public bool HasError => ErrorMessage.Length > 0;
    public bool IsNotBusy => !IsBusy;

    /// <summary>True once a token has been verified, which decides the wording of the final step.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DoneTitle), nameof(DoneBody))]
    private bool _IsConnected;

    public string DoneTitle => IsConnected
        ? AppResources.Onboarding_Done_Title_Connected
        : AppResources.Onboarding_Done_Title_Skipped;

    public string DoneBody => IsConnected
        ? AppResources.Onboarding_Done_Body_Connected
        : AppResources.Onboarding_Done_Body_Skipped;

    public void Load()
    {
        Name = AppSettings.UserName;
        ReminderEnabled = AppSettings.ReminderEnabled;
    }

    [RelayCommand]
    private void Begin() => Step = OnboardingStep.Profile;

    [RelayCommand]
    private void Back()
    {
        ErrorMessage = string.Empty;
        Step = Step == OnboardingStep.Token ? OnboardingStep.Profile : OnboardingStep.Welcome;
    }

    /// <summary>Profile step → token step, persisting the name and reminder choice.</summary>
    [RelayCommand]
    private void Continue()
    {
        AppSettings.UserName = (Name ?? string.Empty).Trim();
        AppSettings.ReminderEnabled = ReminderEnabled;
        ErrorMessage = string.Empty;
        Step = OnboardingStep.Token;
    }

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
                ErrorMessage = AppResources.Onboarding_Token_ErrorRejected;
                return;
            }

            IsConnected = true;
            Step = OnboardingStep.Done;
        }
        catch
        {
            ErrorMessage = AppResources.Onboarding_Token_ErrorRejected;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Skip()
    {
        ErrorMessage = string.Empty;
        IsConnected = false;
        Step = OnboardingStep.Done;
    }

    /// <summary>"I already have a journal" — leave setup without walking the wizard.</summary>
    [RelayCommand]
    private Task ExistingAsync() => FinishAsync();

    [RelayCommand]
    private Task FinishAsync()
    {
        AppSettings.SetupCompleted = true;
        return Shell.Current.GoToAsync($"//{nameof(TodayPage)}");
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        AppSettings.SetupCompleted = true;
        await Shell.Current.GoToAsync($"//{nameof(TodayPage)}");
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}
