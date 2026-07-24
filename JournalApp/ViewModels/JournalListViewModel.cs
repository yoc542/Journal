using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Models;
using JournalApp.Services;
using JournalApp.Views;

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
    private static Task NewEntryAsync() => Shell.Current.GoToAsync($"{nameof(JournalEditorPage)}?id=0");

    [RelayCommand]
    private static Task OpenEntryAsync(JournalEntry entry) =>
        Shell.Current.GoToAsync($"{nameof(JournalEditorPage)}?id={entry.Id}");

    [RelayCommand]
    private async Task ShowMenuAsync(JournalEntry entry)
    {
        var choice = await Shell.Current.DisplayActionSheetAsync(
            entry.DisplayTitle, "Cancel", null, "Upload to Notion", "Delete");

        switch (choice)
        {
            case "Upload to Notion":
                await UploadAsync(entry);
                break;
            case "Delete":
                await DeleteAsync(entry);
                break;
        }
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
            await Shell.Current.DisplayAlertAsync("Imported", $"Imported {imported.Count} entries from Notion.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Import failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAsync(JournalEntry entry)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete entry", "This entry will be permanently deleted.", "Delete", "Cancel");
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
                "Already uploaded", "This entry was already uploaded to Notion. Upload again?", "Upload", "Cancel");
            if (!again)
                return;
        }

        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            await _Notion.UploadEntryAsync(entry);
            entry.IsUploaded = true;
            await _Database.SaveEntryAsync(entry);
            await LoadAsync();
            await Shell.Current.DisplayAlertAsync("Uploaded", "Entry uploaded to Notion.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Upload failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
