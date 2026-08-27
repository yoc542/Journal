using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Models;
using JournalApp.Views;

namespace JournalApp.ViewModels;

public partial class EntryDetailViewModel : ObservableObject
{
    private readonly JournalDatabase _Database;

    private JournalEntry _Entry = new();

    [ObservableProperty] private int _EntryId;
    [ObservableProperty] private string _DateLabel = string.Empty;
    [ObservableProperty] private string _Title = string.Empty;
    [ObservableProperty] private string _MetaLabel = string.Empty;
    [ObservableProperty] private string _Body = string.Empty;
    [ObservableProperty] private string _DeleteMessage = string.Empty;
    [ObservableProperty] private bool _IsDeleteOpen;


    public EntryDetailViewModel(JournalDatabase database) => _Database = database;

    public async Task LoadAsync()
    {
        _Entry = await _Database.GetEntryAsync(EntryId) ?? new JournalEntry();

        DateLabel = _Entry.FullDateLabel;
        Title = _Entry.DisplayTitle;
        Body = _Entry.Text;
        MetaLabel = _Entry.WordCount == 1
            ? string.Format(AppResources.Detail_Meta_One_Format, _Entry.SyncLabel)
            : string.Format(AppResources.Detail_Meta_Format, _Entry.WordCount, _Entry.SyncLabel);
        DeleteMessage = string.Format(AppResources.Detail_Delete_Message_Format, Title, DateLabel);
    }

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    private Task EditAsync() => Shell.Current.GoToAsync($"{nameof(JournalEditorPage)}?id={_Entry.Id}");

    [RelayCommand]
    private void OpenDelete() => IsDeleteOpen = true;

    [RelayCommand]
    private void CloseDelete() => IsDeleteOpen = false;

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        IsDeleteOpen = false;

        if (_Entry.Id != 0)
            await _Database.DeleteEntryAsync(_Entry);

        await Shell.Current.GoToAsync("..");
    }
}
