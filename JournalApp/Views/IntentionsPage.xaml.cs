using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class IntentionsPage : ContentPage
{
    private readonly IntentionsViewModel _ViewModel;

    public IntentionsPage(IntentionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _ViewModel = viewModel;
    }

    // Reloaded on every appearance so day counts and today marks catch up after editing.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _ViewModel.LoadAsync();
    }
}
