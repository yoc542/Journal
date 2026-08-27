using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class NotionConnectPage : ContentPage
{
    private readonly NotionConnectViewModel _ViewModel;

    public NotionConnectPage(NotionConnectViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _ViewModel = viewModel;
    }
}
