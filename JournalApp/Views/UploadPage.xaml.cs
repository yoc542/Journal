using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class UploadPage : ContentPage
{
    private readonly UploadViewModel _ViewModel;

    public UploadPage(UploadViewModel viewModel)
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
