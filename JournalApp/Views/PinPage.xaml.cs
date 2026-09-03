using JournalApp.ViewModels;

namespace JournalApp.Views;

/// <summary>Standalone "set or change your PIN" screen, opened from Settings.</summary>
public partial class PinPage : ContentPage
{
    public PinPage(PinViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
