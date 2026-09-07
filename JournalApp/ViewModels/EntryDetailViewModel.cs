using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Models;
using JournalApp.Services;

namespace JournalApp.ViewModels;

public sealed record IntentionRecap(string Title, string Did, string Evidence);

public partial class EntryDetailViewModel : ObservableObject
{
    private readonly JournalDatabase _Database;
    private readonly NavigationService _Navigation;

    private JournalEntry _Entry = new();

    [ObservableProperty] private int _EntryId;
    [ObservableProperty] private string _DateLabel = string.Empty;
    [ObservableProperty] private string _Title = string.Empty;
    [ObservableProperty] private string _MetaLabel = string.Empty;
    [ObservableProperty] private string _Body = string.Empty;
    [ObservableProperty] private string _DeleteMessage = string.Empty;
    [ObservableProperty] private bool _IsDeleteOpen;
    [ObservableProperty] private ObservableCollection<IntentionRecap> _Intentions = new();
    [ObservableProperty] private bool _HasIntentions;


    public EntryDetailViewModel(JournalDatabase database, NavigationService navigation)
    {
        _Database = database;
        _Navigation = navigation;
    }

    public async Task LoadAsync()
    {
        _Entry = await _Database.GetEntryAsync(EntryId) ?? new JournalEntry();

        DateLabel = _Entry.FullDateLabel;
        Title = _Entry.DayTitle;
        Body = _Entry.Text;
        MetaLabel = _Entry.WordCount == 1
            ? string.Format(AppResources.Detail_Meta_One_Format, _Entry.SyncLabel)
            : string.Format(AppResources.Detail_Meta_Format, _Entry.WordCount, _Entry.SyncLabel);
        DeleteMessage = string.Format(AppResources.Detail_Delete_Message_Format, Title, DateLabel);

        await LoadIntentionsAsync();
    }

    private async Task LoadIntentionsAsync()
    {
        var logs = await _Database.GetLogsForDateAsync(_Entry.EntryDate);
        var intentions = await _Database.GetIntentionsAsync();

        Intentions = new ObservableCollection<IntentionRecap>(
            logs
                .Where(l => l.HasContent)
                .Join(intentions, l => l.IntentionId, i => i.Id, (log, intention) => new IntentionRecap(
                    intention.Title,
                    Spell(log.Did),
                    Spell(log.Evidence))));

        HasIntentions = Intentions.Count > 0;
    }

    /// <summary>One field of a log, or a stand-in when only the other one was filled in.</summary>
    private static string Spell(string value) =>
        value.Trim().Length > 0 ? value.Trim() : AppResources.Intent_Detail_Blank;

    [RelayCommand]
    private Task BackAsync() => _Navigation.BackAsync();

    [RelayCommand]
    private Task EditAsync() =>
        _Navigation.PushAsync(Routes.JournalEditor, new Dictionary<string, object> { ["id"] = _Entry.Id });

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

        await _Navigation.BackAsync();
    }
}
