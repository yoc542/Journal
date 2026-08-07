using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _ViewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _ViewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _ViewModel.LoadAsync();
    }
}
