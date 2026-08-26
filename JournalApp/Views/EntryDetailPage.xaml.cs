using JournalApp.ViewModels;

namespace JournalApp.Views;

public partial class EntryDetailPage : ContentPage, IQueryAttributable
{
    private readonly EntryDetailViewModel _ViewModel;

    public EntryDetailPage(EntryDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _ViewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && int.TryParse(value?.ToString(), out var id))
            _ViewModel.EntryId = id;
    }

    // Reloaded on every appearance so an edit made in the editor shows up on the way back.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _ViewModel.LoadAsync();
    }
}
