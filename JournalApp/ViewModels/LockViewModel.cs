using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Localization;
using JournalApp.Services;
using JournalApp.Views;

namespace JournalApp.ViewModels;

/// <summary>The lock screen: takes the PIN before the journal is reachable.</summary>
public partial class LockViewModel : ObservableObject
{
    /// <summary>Failed attempts before the pad refuses to look at a PIN for a moment.</summary>
    private const int AttemptsBeforeCooldown = 3;

    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(15);

    private int _FailedAttempts;
    private DateTime _BlockedUntil;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnteredLength))]
    private string _Entered = string.Empty;

    [ObservableProperty] private string _ErrorMessage = string.Empty;

    [ObservableProperty] private string _Greeting = string.Empty;

    /// <summary>Digits typed so far, which is all the keypad needs to draw its dots.</summary>
    public int EnteredLength => Entered.Length;

    public void Load()
    {
        var name = AppSettings.UserName;
        Greeting = name.Length > 0
            ? string.Format(AppResources.Lock_Greeting_Format, name)
            : AppResources.Lock_Greeting;

        Entered = string.Empty;
        ErrorMessage = string.Empty;
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

        if (Entered.Length == Constants.PinLength)
            await VerifyAsync(Entered);
    }

    private async Task VerifyAsync(string pin)
    {
        var stored = await SecureSettings.GetPinAsync();

        // The flag says a PIN exists but the keystore cannot produce it (wiped or undecryptable).
        // Refusing entry would strand the user in front of their own journal, so drop the lock.
        if (stored.Length == 0)
        {
            AppSettings.PinSet = false;
            await UnlockAsync();
            return;
        }

        if (DateTime.UtcNow < _BlockedUntil)
        {
            Entered = string.Empty;
            ErrorMessage = AppResources.Lock_Error_Wait;
            return;
        }

        if (pin != stored)
        {
            _FailedAttempts++;
            Entered = string.Empty;

            if (_FailedAttempts >= AttemptsBeforeCooldown)
            {
                _BlockedUntil = DateTime.UtcNow + Cooldown;
                ErrorMessage = AppResources.Lock_Error_Wait;
            }
            else
            {
                ErrorMessage = AppResources.Lock_Error;
            }

            return;
        }

        _FailedAttempts = 0;
        _BlockedUntil = default;
        Entered = string.Empty;
        await UnlockAsync();
    }

    private static Task UnlockAsync() => Shell.Current.GoToAsync($"//{nameof(TodayPage)}");
}
