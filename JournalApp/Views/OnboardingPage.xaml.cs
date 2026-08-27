using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _ViewModel;

    public OnboardingPage(OnboardingViewModel viewModel)
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
