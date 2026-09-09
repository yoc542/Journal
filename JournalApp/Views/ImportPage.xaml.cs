using System.ComponentModel;
using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class ImportPage : ContentPage
{
    private readonly ImportViewModel _ViewModel;
    private string? _Step;

    public ImportPage(ImportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _ViewModel = viewModel;

        _ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ShowCurrentStep();
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ImportViewModel.Step) or nameof(ImportViewModel.IsConnected))
            ShowCurrentStep();
    }

    /// <summary>
    /// Builds the one step the view model is on and drops the previous one. Only the active step
    /// may exist: an inactive step left in the tree is still measured on iOS, at width 0, where a
    /// wrapping Label carrying LineHeight never settles and pins the main thread in a layout loop.
    /// </summary>
    private void ShowCurrentStep()
    {
        var step = CurrentStep;
        if (step == _Step)
            return;

        _Step = step;
        StepHost.Content = (View)((DataTemplate)Resources[step]).CreateContent();
    }

    private string CurrentStep =>
        _ViewModel.IsNotConnected ? "NotConnectedStep"
        : _ViewModel.IsPicker ? "PickerStep"
        : _ViewModel.IsConflict ? "ConflictStep"
        : _ViewModel.IsFailed ? "FailedStep"
        : _ViewModel.IsDone ? "DoneStep"
        : "ScanningStep";
}
