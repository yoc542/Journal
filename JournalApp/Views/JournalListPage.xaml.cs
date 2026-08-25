using JournalApp.Localization;
using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class JournalListPage : ContentPage
{
    private readonly JournalListViewModel _ViewModel;

    public JournalListPage(JournalListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _ViewModel = viewModel;

        if (Constants.DeveloperMode)
            ToolbarItems.Add(new ToolbarItem { Text = AppResources.Import_ToolbarItem, Command = viewModel.ImportFromNotionCommand });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _ViewModel.LoadCommand.ExecuteAsync(null);
    }
}
