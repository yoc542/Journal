using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class LockPage : ContentPage
{
    private readonly LockViewModel _ViewModel;

    public LockPage(LockViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _ViewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ViewModel.Load();
    }
}
