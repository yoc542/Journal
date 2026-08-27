using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class ImportPage : ContentPage
{
    private readonly ImportViewModel _ViewModel;

    public ImportPage(ImportViewModel viewModel)
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
