using JournalApp.ViewModels;

namespace JournalApp.Views;

/// <summary>Standalone "set or change your PIN" screen, opened from Settings.</summary>
public partial class PinPage : ContentPage
{
    private readonly PinViewModel _ViewModel;

    public PinPage(PinViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _ViewModel = viewModel;
        _ViewModel.PinSaved += OnPinSaved;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ViewModel.Load();
    }

    /// <summary>The PIN is stored, so this screen has nothing left to do.</summary>
    private async void OnPinSaved(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
