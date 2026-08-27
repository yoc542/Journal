using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _ViewModel;

    public TodayPage(TodayViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _ViewModel = viewModel;
    }

    // Reloaded on every appearance so counts and the week strip catch up after editing.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _ViewModel.LoadAsync();
    }
}
