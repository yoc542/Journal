using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Localization;
using JournalApp.Services;

namespace JournalApp.ViewModels;

/// <summary>
/// "Choose a PIN, then type it again" — behind <see cref="Views.PinPage"/>, and reused by the
/// onboarding wizard for its PIN step.
/// </summary>
public partial class PinViewModel : ObservableObject
{
    /// <summary>First pass, kept only until the confirmation matches it.</summary>
    private string _FirstPass = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnteredLength))]
    private string _Entered = string.Empty;

    [ObservableProperty] private string _ErrorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Eyebrow), nameof(Title), nameof(Body))]
    private bool _IsConfirming;

    /// <summary>True when replacing an existing PIN rather than choosing the first one.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Eyebrow), nameof(Title))]
    private bool _IsChanging;

    /// <summary>
    /// Run once the new PIN has been confirmed and saved. Defaults to leaving the screen, which is
    /// what <see cref="Views.PinPage"/> wants; the onboarding wizard replaces it with its own step.
    /// </summary>
    public Func<Task> Completed { get; set; } = () => Shell.Current.GoToAsync("..");

    public PinViewModel() => Load();

    /// <summary>Digits typed so far, which is all the keypad needs to draw its dots.</summary>
    public int EnteredLength => Entered.Length;

    public string Eyebrow => IsConfirming ? AppResources.Pin_Confirm_Eyebrow
        : IsChanging ? AppResources.Pin_Change_Eyebrow
        : AppResources.Pin_Setup_Eyebrow;

    public string Title => IsConfirming ? AppResources.Pin_Confirm_Title
        : IsChanging ? AppResources.Pin_Change_Title
        : AppResources.Pin_Setup_Title;

    public string Body => IsConfirming ? AppResources.Pin_Confirm_Body : AppResources.Pin_Setup_Body;

    /// <summary>Puts the pad back to its opening state, so re-entering the screen starts clean.</summary>
    public void Load()
    {
        IsChanging = AppSettings.PinSet;
        Restart(string.Empty);
    }

    [RelayCommand]
    private async Task KeyAsync(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        if (key == Constants.PinBackspaceKey)
        {
            ErrorMessage = string.Empty;
            if (Entered.Length > 0)
                Entered = Entered[..^1];
            return;
        }

        if (Entered.Length >= Constants.PinLength)
            return;

        ErrorMessage = string.Empty;
        Entered += key;

        if (Entered.Length < Constants.PinLength)
            return;

        // First pass done — ask for it again rather than saving what might be a typo.
        if (!IsConfirming)
        {
            _FirstPass = Entered;
            IsConfirming = true;
            Entered = string.Empty;
            return;
        }

        if (Entered != _FirstPass)
        {
            Restart(AppResources.Pin_Error_Mismatch);
            return;
        }

        await SecureSettings.SetPinAsync(Entered);
        Restart(string.Empty);
        await Completed();
    }

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");

    /// <summary>Back to choosing a first PIN, optionally explaining why.</summary>
    private void Restart(string error)
    {
        _FirstPass = string.Empty;
        IsConfirming = false;
        Entered = string.Empty;
        ErrorMessage = error;
    }
}
