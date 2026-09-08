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

    /// <summary>
    /// The scan is started from <see cref="OnNavigatedTo"/> rather than OnAppearing: on iOS the
    /// latter is ViewWillAppear, so the page is still mid-push while the Notion request runs and
    /// anything the load does to the navigation stack deadlocks UIKit. OnNavigatedTo fires once the
    /// transition has settled. The load reports its own failures, so it is safe to leave unawaited.
    /// </summary>
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _ = _ViewModel.LoadAsync();
    }
}
