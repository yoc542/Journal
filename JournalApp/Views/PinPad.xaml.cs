using System.Windows.Input;

namespace JournalApp.Views;

/// <summary>One position on the PIN pad's dot row.</summary>
/// <param name="IsFilled">True once a digit has been typed into this position.</param>
public sealed record PinDot(bool IsFilled);

/// <summary>
/// The dots-plus-keypad half of a PIN screen, shared by the lock screen, the settings PIN screen
/// and the onboarding wizard's PIN step. It owns nothing but its own appearance: key presses go out
/// through <see cref="KeyCommand"/> and the host decides what they mean.
/// </summary>
public partial class PinPad : ContentView
{
    /// <summary>How many digits have been typed, which fills that many dots.</summary>
    public static readonly BindableProperty FilledCountProperty = BindableProperty.Create(
        nameof(FilledCount), typeof(int), typeof(PinPad), 0, propertyChanged: OnFilledCountChanged);

    /// <summary>Run for every key press, with the digit — or <see cref="Constants.PinBackspaceKey"/> — as its parameter.</summary>
    public static readonly BindableProperty KeyCommandProperty = BindableProperty.Create(
        nameof(KeyCommand), typeof(ICommand), typeof(PinPad));

    /// <summary>Message shown between the dots and the keypad; empty hides the banner.</summary>
    public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(
        nameof(ErrorMessage), typeof(string), typeof(PinPad), string.Empty,
        propertyChanged: OnErrorMessageChanged);

    public PinPad()
    {
        InitializeComponent();

        // The markup below binds to the control, not to whatever view model the page is showing.
        Body.BindingContext = this;
    }

    public int FilledCount
    {
        get => (int)GetValue(FilledCountProperty);
        set => SetValue(FilledCountProperty, value);
    }

    public ICommand? KeyCommand
    {
        get => (ICommand?)GetValue(KeyCommandProperty);
        set => SetValue(KeyCommandProperty, value);
    }

    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    /// <summary>The dot row, rebuilt whenever <see cref="FilledCount"/> changes.</summary>
    public IReadOnlyList<PinDot> Dots { get; private set; } = BuildDots(0);

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private static void OnFilledCountChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var pad = (PinPad)bindable;
        pad.Dots = BuildDots((int)newValue);
        pad.OnPropertyChanged(nameof(Dots));
    }

    private static void OnErrorMessageChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((PinPad)bindable).OnPropertyChanged(nameof(HasError));

    private static PinDot[] BuildDots(int filled) =>
        Enumerable.Range(0, Constants.PinLength).Select(i => new PinDot(i < filled)).ToArray();
}
