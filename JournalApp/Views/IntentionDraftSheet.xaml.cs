using System.Windows.Input;
using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class IntentionDraftSheet : ContentView
{
    public static readonly BindableProperty DraftProperty = BindableProperty.Create(
        nameof(Draft), typeof(IntentionDraft), typeof(IntentionDraftSheet));

    public static readonly BindableProperty SaveCommandProperty = BindableProperty.Create(
        nameof(SaveCommand), typeof(ICommand), typeof(IntentionDraftSheet));

    public static readonly BindableProperty CancelCommandProperty = BindableProperty.Create(
        nameof(CancelCommand), typeof(ICommand), typeof(IntentionDraftSheet));

    public IntentionDraftSheet()
    {
        InitializeComponent();

        // The markup binds to the control, not to whatever view model the host page is showing.
        Body.BindingContext = this;
    }

    public IntentionDraft? Draft
    {
        get => (IntentionDraft?)GetValue(DraftProperty);
        set => SetValue(DraftProperty, value);
    }

    public ICommand? SaveCommand
    {
        get => (ICommand?)GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }
}
