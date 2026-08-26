using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Models;
using JournalApp.Services;
using JournalApp.Views;

namespace JournalApp.ViewModels;

public partial class EntryDetailViewModel : ObservableObject
{
    private readonly JournalDatabase _Database;
    private readonly NotionService _Notion;

    private JournalEntry _Entry = new();

    [ObservableProperty] private int _EntryId;
    [ObservableProperty] private string _DateLabel = string.Empty;
    [ObservableProperty] private string _Title = string.Empty;
    [ObservableProperty] private string _MetaLabel = string.Empty;
    [ObservableProperty] private string _Body = string.Empty;
    [ObservableProperty] private string _DeleteMessage = string.Empty;
    [ObservableProperty] private bool _IsDeleteOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _IsBusy;

    public EntryDetailViewModel(JournalDatabase database, NotionService notion)
    {
        _Database = database;
        _Notion = notion;
    }

    public bool IsNotBusy => !IsBusy;

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

    /// <summary>Interim per-entry upload. The design routes this through the Notion group's
    /// upload screen, which does not exist yet.</summary>
    [RelayCommand]
    private async Task UploadAsync()
    {
        if (IsBusy || _Entry.Id == 0)
            return;

        if (_Entry.IsUploaded)
        {
            var again = await Shell.Current.DisplayAlertAsync(
                AppResources.Upload_AlreadyTitle, AppResources.Upload_AlreadyMessage,
                AppResources.Upload_Confirm, AppResources.Menu_Cancel);
            if (!again)
                return;
        }

        try
        {
            IsBusy = true;
            _Entry.NotionPageId = await _Notion.UploadEntryAsync(_Entry);
            _Entry.IsUploaded = true;
            await _Database.SaveEntryAsync(_Entry);
            await LoadAsync();
            await Shell.Current.DisplayAlertAsync(
                AppResources.Upload_SuccessTitle, AppResources.Upload_SuccessMessage, AppResources.OK);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(AppResources.Upload_FailTitle, ex.Message, AppResources.OK);
        }
        finally
        {
            IsBusy = false;
        }
    }

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
