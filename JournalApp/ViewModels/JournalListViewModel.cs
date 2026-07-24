using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Models;
using JournalApp.Resources.Strings;
using JournalApp.Services;

namespace JournalApp.ViewModels;

public partial class JournalListViewModel : ObservableObject
{
    private readonly JournalDatabase _Database;
    private readonly NotionService _Notion;

    [ObservableProperty] private ObservableCollection<JournalEntry> _Entries = new();
    [ObservableProperty] private bool _IsBusy;

    public JournalListViewModel(JournalDatabase database, NotionService notion)
    {
        _Database = database;
        _Notion = notion;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var items = await _Database.GetEntriesAsync();
        Entries = new ObservableCollection<JournalEntry>(items);
    }

    [RelayCommand]
    private static Task OpenEntryAsync(JournalEntry entry) =>
        Shell.Current.GoToAsync($"..?id={entry.Id}");

    [RelayCommand]
    private async Task ShowMenuAsync(JournalEntry entry)
    {
        var choice = await Shell.Current.DisplayActionSheetAsync(
            entry.DisplayTitle, AppResources.Menu_Cancel, null,
            AppResources.Menu_UploadToNotion, AppResources.Menu_Delete);

        if (choice == AppResources.Menu_UploadToNotion)
            await UploadAsync(entry);
        else if (choice == AppResources.Menu_Delete)
            await DeleteAsync(entry);
    }

    [RelayCommand]
    private async Task ImportFromNotionAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            var imported = await _Notion.FetchEntriesAsync();
            foreach (var entry in imported)
            {
                entry.IsUploaded = true;
                await _Database.SaveEntryAsync(entry);
            }
            await LoadAsync();
            await Shell.Current.DisplayAlertAsync(
                AppResources.Import_SuccessTitle,
                string.Format(AppResources.Import_SuccessMessage_Format, imported.Count),
                AppResources.OK);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(AppResources.Import_FailTitle, ex.Message, AppResources.OK);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAsync(JournalEntry entry)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            AppResources.Delete_Title, AppResources.Delete_Message, AppResources.Delete_Confirm, AppResources.Menu_Cancel);
        if (!confirmed)
            return;

        await _Database.DeleteEntryAsync(entry);
        Entries.Remove(entry);
    }

    private async Task UploadAsync(JournalEntry entry)
    {
        if (entry.IsUploaded)
        {
            var again = await Shell.Current.DisplayAlertAsync(
                AppResources.Upload_AlreadyTitle, AppResources.Upload_AlreadyMessage,
                AppResources.Upload_Confirm, AppResources.Menu_Cancel);
            if (!again)
                return;
        }

        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            entry.NotionPageId = await _Notion.UploadEntryAsync(entry);
            entry.IsUploaded = true;
            await _Database.SaveEntryAsync(entry);
            await LoadAsync();
            await Shell.Current.DisplayAlertAsync(AppResources.Upload_SuccessTitle, AppResources.Upload_SuccessMessage, AppResources.OK);
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
}
