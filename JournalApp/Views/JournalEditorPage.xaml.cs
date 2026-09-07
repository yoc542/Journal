using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class JournalEditorPage : ContentPage, IQueryAttributable
{
    private readonly JournalEditorViewModel _ViewModel;

    public JournalEditorPage(JournalEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _ViewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && int.TryParse(value?.ToString(), out var id))
            _ViewModel.EntryId = id;

        // Today sends the user straight to the picker when nothing is chosen for the day yet.
        if (query.ContainsKey("picker"))
            _ViewModel.OpenPickerOnLoad = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _ViewModel.LoadAsync();
    }

    // Notepad-style: auto-save whenever the user leaves the editor.
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _ViewModel.SaveAsync();
    }
}
