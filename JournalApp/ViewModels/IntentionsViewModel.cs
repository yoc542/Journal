using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Models;
using JournalApp.Services;

namespace JournalApp.ViewModels;

/// <summary>One intention as the library lists it.</summary>
public partial class IntentionRow : ObservableObject
{
    public IntentionRow(Intention intention, int loggedDays, bool isTrackedToday)
    {
        Intention = intention;
        LoggedDays = loggedDays;
        _IsTrackedToday = isTrackedToday;
        MetaLabel = BuildMeta(intention, loggedDays);
    }

    public Intention Intention { get; }

    /// <summary>Days actually written about, which is what the meta line reports.</summary>
    public int LoggedDays { get; }

    public string Title => Intention.Title;
    public string NoteLabel => Intention.NoteLabel;
    public string MetaLabel { get; }
    public bool IsActive => Intention.IsActive;
    public bool IsCompleted => !Intention.IsActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackLabel), nameof(BadgeLabel))]
    private bool _IsTrackedToday;

    public string TrackLabel => IsTrackedToday ? AppResources.Intent_Tracking : AppResources.Intent_Track;

    public string BadgeLabel => IsCompleted ? AppResources.Intent_Badge_Complete
        : IsTrackedToday ? AppResources.Intent_Badge_OnToday
        : string.Empty;

    private static string BuildMeta(Intention intention, int loggedDays) => intention.IsActive
        ? loggedDays switch
        {
            0 => string.Format(AppResources.Intent_Meta_Active_None_Format, intention.SinceLabel),
            1 => string.Format(AppResources.Intent_Meta_Active_One_Format, intention.SinceLabel),
            _ => string.Format(AppResources.Intent_Meta_Active_Format, intention.SinceLabel, loggedDays),
        }
        : loggedDays switch
        {
            0 => string.Format(AppResources.Intent_Meta_Completed_None_Format, intention.CompletedLabel),
            1 => string.Format(AppResources.Intent_Meta_Completed_One_Format, intention.CompletedLabel),
            _ => string.Format(AppResources.Intent_Meta_Completed_Format, intention.CompletedLabel, loggedDays),
        };
}

public partial class IntentionsViewModel : ObservableObject
{
    private readonly JournalDatabase _Database;
    private readonly NavigationService _Navigation;

    private List<Intention> _All = new();
    private Dictionary<int, int> _Counts = new();
    private HashSet<int> _TrackedToday = new();

    /// <summary>The row waiting on the delete sheet's answer.</summary>
    private IntentionRow? _Doomed;

    [ObservableProperty] private ObservableCollection<IntentionRow> _Rows = new();
    [ObservableProperty] private string _ActiveTabLabel = string.Empty;
    [ObservableProperty] private string _CompletedTabLabel = string.Empty;
    [ObservableProperty] private string _EmptyTitle = string.Empty;
    [ObservableProperty] private string _EmptyBody = string.Empty;
    [ObservableProperty] private string _DeleteMessage = string.Empty;
    [ObservableProperty] private bool _IsDeleteOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowingCompleted))]
    private bool _ShowingActive = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRows))]
    private bool _IsEmpty;

    public IntentionsViewModel(JournalDatabase database, NavigationService navigation)
    {
        _Database = database;
        _Navigation = navigation;
    }

    public IntentionDraft Draft { get; } = new();

    public bool ShowingCompleted => !ShowingActive;
    public bool HasRows => !IsEmpty;

    public async Task LoadAsync()
    {
        _All = await _Database.GetIntentionsAsync();
        _Counts = await _Database.GetLoggedDayCountsAsync();
        _TrackedToday = await TrackedTodayAsync();

        Rebuild();
    }

    /// <summary>Re-projects the loaded intentions onto whichever tab is showing.</summary>
    private void Rebuild()
    {
        var active = _All.Count(i => i.IsActive);
        ActiveTabLabel = string.Format(AppResources.Intent_Tab_Active_Format, active);
        CompletedTabLabel = string.Format(AppResources.Intent_Tab_Completed_Format, _All.Count - active);

        var shown = ShowingActive
            ? _All.Where(i => i.IsActive)
            : _All.Where(i => !i.IsActive).OrderByDescending(i => i.CompletedAt ?? i.CreatedAt);

        Rows = new ObservableCollection<IntentionRow>(shown.Select(i =>
            new IntentionRow(i, _Counts.GetValueOrDefault(i.Id), _TrackedToday.Contains(i.Id))));

        IsEmpty = Rows.Count == 0;
        EmptyTitle = ShowingActive ? AppResources.Intent_Empty_Active_Title : AppResources.Intent_Empty_Done_Title;
        EmptyBody = ShowingActive ? AppResources.Intent_Empty_Active_Body : AppResources.Intent_Empty_Done_Body;
    }

    [RelayCommand]
    private void ShowActive()
    {
        ShowingActive = true;
        Rebuild();
    }

    [RelayCommand]
    private void ShowCompleted()
    {
        ShowingActive = false;
        Rebuild();
    }

    [RelayCommand]
    private void NewIntention() => Draft.OpenNew();

    [RelayCommand]
    private void EditIntention(IntentionRow row) => Draft.OpenEdit(row.Intention);

    [RelayCommand]
    private void CancelDraft() => Draft.Close();

    [RelayCommand]
    private async Task SaveDraftAsync()
    {
        if (!Draft.Validate())
            return;

        var intention = Draft.EditingId == 0
            ? new Intention()
            : await _Database.GetIntentionAsync(Draft.EditingId) ?? new Intention();

        Draft.ApplyTo(intention);
        await _Database.SaveIntentionAsync(intention);

        Draft.Close();
        await LoadAsync();
    }

    /// <summary>Picks this intention for today, or un-picks it, by adding or removing the day's log.</summary>
    [RelayCommand]
    private async Task ToggleTrackAsync(IntentionRow row)
    {
        var existing = await FindTodayLogAsync(row.Intention.Id);

        if (existing is not null)
            await _Database.DeleteLogAsync(existing);
        else
            await _Database.SaveLogAsync(new IntentionLog
            {
                IntentionId = row.Intention.Id,
                EntryDate = DateTime.Today,
            });

        row.IsTrackedToday = existing is null;
        _TrackedToday = await TrackedTodayAsync();
    }

    [RelayCommand]
    private Task CompleteAsync(IntentionRow row) => SetStatusAsync(row, IntentionStatus.Completed);

    [RelayCommand]
    private Task ReopenAsync(IntentionRow row) => SetStatusAsync(row, IntentionStatus.Active);

    /// <summary>Completing also drops an untouched pick from today, since a finished thing is no
    /// longer being worked on. A day already written about is left exactly as it was.</summary>
    private async Task SetStatusAsync(IntentionRow row, IntentionStatus status)
    {
        var intention = row.Intention;
        intention.Status = status;
        intention.CompletedAt = status == IntentionStatus.Completed ? DateTime.Now : null;
        await _Database.SaveIntentionAsync(intention);

        if (status == IntentionStatus.Completed)
        {
            var today = await FindTodayLogAsync(intention.Id);
            if (today is not null && !today.HasContent)
                await _Database.DeleteLogAsync(today);
        }

        await LoadAsync();
    }

    [RelayCommand]
    private void OpenDelete(IntentionRow row)
    {
        _Doomed = row;
        DeleteMessage = row.LoggedDays switch
        {
            0 => string.Format(AppResources.Intent_Delete_Message_None_Format, row.Title),
            1 => string.Format(AppResources.Intent_Delete_Message_One_Format, row.Title),
            _ => string.Format(AppResources.Intent_Delete_Message_Format, row.Title, row.LoggedDays),
        };
        IsDeleteOpen = true;
    }

    [RelayCommand]
    private void CloseDelete()
    {
        _Doomed = null;
        IsDeleteOpen = false;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        var row = _Doomed;
        _Doomed = null;
        IsDeleteOpen = false;

        if (row is null)
            return;

        await _Database.DeleteIntentionAsync(row.Intention);
        await LoadAsync();
    }

    [RelayCommand]
    private Task BackAsync() => _Navigation.BackAsync();

    private async Task<IntentionLog?> FindTodayLogAsync(int intentionId)
    {
        var logs = await _Database.GetLogsForDateAsync(DateTime.Today);
        return logs.FirstOrDefault(l => l.IntentionId == intentionId);
    }

    private async Task<HashSet<int>> TrackedTodayAsync()
    {
        var logs = await _Database.GetLogsForDateAsync(DateTime.Today);
        return logs.Select(l => l.IntentionId).ToHashSet();
    }
}
