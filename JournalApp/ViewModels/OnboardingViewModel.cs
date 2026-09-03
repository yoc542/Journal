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
    Pin,
    Token,
    Done,
}

public partial class OnboardingViewModel : ObservableObject
{
    private readonly NotionService _Notion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWelcome), nameof(IsProfile), nameof(IsPin), nameof(IsToken),
        nameof(IsDone), nameof(StepLabel))]
    private OnboardingStep _Step = OnboardingStep.Welcome;

    [ObservableProperty] private string _Name = string.Empty;
    [ObservableProperty] private string _Token = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _ErrorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _IsBusy;

    public OnboardingViewModel(NotionService notion)
    {
        _Notion = notion;
        Pin.Completed = () =>
        {
            Step = OnboardingStep.Token;
            return Task.CompletedTask;
        };
    }

    /// <summary>Drives the keypad shown on the PIN step; the wizard moves on once it saves.</summary>
    public PinViewModel Pin { get; } = new();

    public int MaxUserNameLength => Constants.MaxUserNameLength;

    public bool IsWelcome => Step == OnboardingStep.Welcome;
    public bool IsProfile => Step == OnboardingStep.Profile;
    public bool IsPin => Step == OnboardingStep.Pin;
    public bool IsToken => Step == OnboardingStep.Token;
    public bool IsDone => Step == OnboardingStep.Done;

    public string StepLabel => string.Format(AppResources.Onboarding_Step_Format, Step switch
    {
        OnboardingStep.Pin => 2,
        OnboardingStep.Token => 3,
        _ => 1,
    });

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
    }

    [RelayCommand]
    private void Begin() => Step = OnboardingStep.Profile;

    [RelayCommand]
    private void Back()
    {
        ErrorMessage = string.Empty;
        Step = Step switch
        {
            OnboardingStep.Token => OnboardingStep.Pin,
            OnboardingStep.Pin => OnboardingStep.Profile,
            _ => OnboardingStep.Welcome,
        };

        if (Step == OnboardingStep.Pin)
            Pin.Load();
    }

    /// <summary>Profile step → PIN step, persisting the name.</summary>
    [RelayCommand]
    private void Continue()
    {
        AppSettings.UserName = (Name ?? string.Empty).Trim();
        ErrorMessage = string.Empty;
        Pin.Load();
        Step = OnboardingStep.Pin;
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
