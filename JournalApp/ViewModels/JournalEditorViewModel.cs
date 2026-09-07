using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Models;
using JournalApp.Services;

namespace JournalApp.ViewModels;

public partial class IntentionLogRow : ObservableObject
{
    private readonly Action _OnEdited;

    public IntentionLogRow(Intention intention, IntentionLog log, Action onEdited)
    {
        Intention = intention;
        Log = log;
        _OnEdited = onEdited;
        _Did = log.Did;
        _Evidence = log.Evidence;
    }

    public Intention Intention { get; }
    public IntentionLog Log { get; }

    public string Title => Intention.Title;
    public string NoteLabel => Intention.NoteLabel;
    public int MaxLength => Constants.MaxIntentionLogLength;

    [ObservableProperty] private string _Did;
    [ObservableProperty] private string _Evidence;

    /// <summary>Whether the fields have moved away from what is on disk.</summary>
    public bool IsDirty => Log.Did != Trimmed(Did) || Log.Evidence != Trimmed(Evidence);

    public bool HasContent => Trimmed(Did).Length > 0 || Trimmed(Evidence).Length > 0;

    public void Commit()
    {
        Log.Did = Trimmed(Did);
        Log.Evidence = Trimmed(Evidence);
    }

    // Every keystroke rides the page's own debounced autosave.
    partial void OnDidChanged(string value) => _OnEdited();

    partial void OnEvidenceChanged(string value) => _OnEdited();

    private static string Trimmed(string? value) => (value ?? string.Empty).Trim();
}

/// <summary>One choice in the "what were you working toward?" sheet.</summary>
public partial class IntentionPickRow : ObservableObject
{
    public IntentionPickRow(Intention intention, bool isSelected)
    {
        Intention = intention;
        _IsSelected = isSelected;
    }

    public Intention Intention { get; }

    public string Title => Intention.Title;
    public string NoteLabel => Intention.NoteLabel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckMark))]
    private bool _IsSelected;

    public string CheckMark => IsSelected ? "✓" : string.Empty;
}

public partial class JournalEditorViewModel : ObservableObject
{
    /// <summary>Quiet period after the last keystroke before the page is written to disk.</summary>
    private const int AutoSaveDelayMs = 900;

    private readonly JournalDatabase _Database;
    private readonly NavigationService _Navigation;

    private JournalEntry _Entry = new();
    private CancellationTokenSource? _AutoSave;
    private bool _IsLoading;

    /// <summary>The row waiting on the "remove this from the day?" sheet.</summary>
    private IntentionLogRow? _Unpicking;

    [ObservableProperty] private int _EntryId;
    [ObservableProperty] private string _Text = string.Empty;
    [ObservableProperty] private string _DateLabel = string.Empty;
    [ObservableProperty] private string _HeaderTitle = string.Empty;
    [ObservableProperty] private string _BackLabel = string.Empty;
    [ObservableProperty] private string _SaveStatus = string.Empty;
    [ObservableProperty] private bool _IsSaving;

    // --- what I wanted ---
    [ObservableProperty] private ObservableCollection<IntentionLogRow> _Logs = new();
    [ObservableProperty] private ObservableCollection<IntentionPickRow> _PickerRows = new();
    [ObservableProperty] private string _PickerDoneLabel = string.Empty;
    [ObservableProperty] private string _UnpickMessage = string.Empty;
    [ObservableProperty] private bool _IsPickerOpen;
    [ObservableProperty] private bool _IsUnpickOpen;
    [ObservableProperty] private bool _PickerIsEmpty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoLogs))]
    private bool _HasLogs;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WordCountLabel))]
    private int _WordCount;

    public JournalEditorViewModel(JournalDatabase database, NavigationService navigation)
    {
        _Database = database;
        _Navigation = navigation;
    }

    /// <summary>Set by the Today screen when it sends the user here to choose what they were working toward.</summary>
    public bool OpenPickerOnLoad { get; set; }

    public IntentionDraft Draft { get; } = new();

    public bool IsNotBusy => !IsBusy;
    public bool HasNoLogs => !HasLogs;

    public int MaxLength => Constants.MaxJournalLength;
    public string PlaceholderText => AppResources.Editor_Placeholder;

    public string WordCountLabel => WordCount == 1
        ? AppResources.Editor_WordCount_One
        : string.Format(AppResources.Editor_WordCount_Format, WordCount);

    partial void OnTextChanged(string value)
    {
        if (_IsLoading)
            return;

        WordCount = CountWords(value ?? string.Empty);
        MarkEdited();
    }

    public async Task LoadAsync()
    {
        _IsLoading = true;
        try
        {
            _Entry = EntryId != 0
                ? await _Database.GetEntryAsync(EntryId) ?? new JournalEntry()
                : await _Database.GetEntryForDateAsync(DateTime.Today) ?? new JournalEntry();

            EntryId = _Entry.Id;
            Text = _Entry.Text;
        }
        finally
        {
            _IsLoading = false;
        }

        var isToday = _Entry.EntryDate.Date == DateTime.Today;

        WordCount = CountWords(Text);
        DateLabel = _Entry.EntryDate.ToString("dddd, d MMMM", CultureInfo.CurrentCulture);

        // Reached from Today for tonight's page, but from the detail screen for an older one.
        BackLabel = isToday ? AppResources.Editor_Back : AppResources.Editor_Back_Generic;
        HeaderTitle = isToday
            ? AppResources.Editor_Tonight_Title
            : string.Format(AppResources.Entry_Day_Format, _Entry.DayNumber);

        IsSaving = false;
        SaveStatus = _Entry.Id == 0 ? AppResources.Saved_Never : FormatSavedAt(_Entry.UpdatedAt);

        await LoadLogsAsync();

        if (OpenPickerOnLoad)
        {
            OpenPickerOnLoad = false;
            await OpenPickerAsync();
        }
    }

    /// <summary>Loads the intentions picked for this page's day, in the order they were picked.</summary>
    private async Task LoadLogsAsync()
    {
        var logs = await _Database.GetLogsForDateAsync(_Entry.EntryDate);
        var intentions = await _Database.GetIntentionsAsync();

        Logs = new ObservableCollection<IntentionLogRow>(
            logs.Join(intentions, l => l.IntentionId, i => i.Id, (l, i) => new IntentionLogRow(i, l, MarkEdited)));

        HasLogs = Logs.Count > 0;
    }

    /// <summary>Debounced autosave — the newest keystroke supersedes any pending write.</summary>
    private void QueueAutoSave()
    {
        _AutoSave?.Cancel();
        var cts = new CancellationTokenSource();
        _AutoSave = cts;
        _ = AutoSaveAsync(cts.Token);
    }

    /// <summary>Shows the page as unsaved and schedules the write. Shared by the prose and the log fields.</summary>
    private void MarkEdited()
    {
        if (_IsLoading)
            return;

        IsSaving = true;
        SaveStatus = AppResources.Editor_Saving;
        QueueAutoSave();
    }

    private async Task AutoSaveAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(AutoSaveDelayMs, token);
            await MainThread.InvokeOnMainThreadAsync(SaveAsync);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a later keystroke, or the page went away.
        }
    }

    /// <summary>Persists the page and refreshes the save status. Any pending autosave is dropped.</summary>
    public async Task SaveAsync()
    {
        _AutoSave?.Cancel();

        var text = (Text ?? string.Empty).Trim();
        var logsChanged = await SaveLogsAsync();
        var hasLoggedSomething = Logs.Any(l => l.HasContent);

        // Nothing worth a row yet: a brand-new page with neither prose nor anything logged against it.
        if (_Entry.Id == 0 && text.Length == 0 && !hasLoggedSomething)
        {
            IsSaving = false;
            SaveStatus = AppResources.Saved_Never;
            return;
        }

        // A changed log makes the day stale in Notion just as changed prose does.
        if (_Entry.Id == 0 || _Entry.Text != text || logsChanged)
        {
            _Entry.Text = text;
            _Entry.IsUploaded = false;
            await _Database.SaveEntryAsync(_Entry);
            EntryId = _Entry.Id;
        }

        IsSaving = false;
        SaveStatus = FormatSavedAt(_Entry.UpdatedAt);
    }

    private async Task<bool> SaveLogsAsync()
    {
        var changed = false;

        foreach (var row in Logs.Where(r => r.IsDirty))
        {
            row.Commit();
            await _Database.SaveLogAsync(row.Log);
            changed = true;
        }

        return changed;
    }

    // --- what I wanted ---

    /// <summary>Opens the picker over the intentions still being worked on.</summary>
    [RelayCommand]
    private async Task OpenPickerAsync()
    {
        await SaveAsync();

        var active = await _Database.GetActiveIntentionsAsync();
        var picked = Logs.Select(l => l.Intention.Id).ToHashSet();

        PickerRows = new ObservableCollection<IntentionPickRow>(
            active.Select(i => new IntentionPickRow(i, picked.Contains(i.Id))));

        PickerIsEmpty = PickerRows.Count == 0;
        RefreshPickerLabel();
        IsPickerOpen = true;
    }

    [RelayCommand]
    private void ClosePicker() => IsPickerOpen = false;

    /// <summary>Adds the day's log for this intention, or asks before taking a written one away.</summary>
    [RelayCommand]
    private async Task TogglePickAsync(IntentionPickRow row)
    {
        var existing = Logs.FirstOrDefault(l => l.Intention.Id == row.Intention.Id);

        if (existing is null)
        {
            await _Database.SaveLogAsync(new IntentionLog
            {
                IntentionId = row.Intention.Id,
                EntryDate = _Entry.EntryDate.Date,
            });

            row.IsSelected = true;
            await LoadLogsAsync();
            RefreshPickerLabel();
            return;
        }

        await RemoveLogAsync(existing);
        row.IsSelected = Logs.Any(l => l.Intention.Id == row.Intention.Id);
        RefreshPickerLabel();
    }

    /// <summary>The card's own "×" — the same removal, from the page rather than the sheet.</summary>
    [RelayCommand]
    private Task UnpickAsync(IntentionLogRow row) => RemoveLogAsync(row);

    /// <summary>Drops an untouched pick outright; anything written is worth a question first.</summary>
    private async Task RemoveLogAsync(IntentionLogRow row)
    {
        // The list is about to be rebuilt, so commit whatever is typed into the other cards first.
        await SaveAsync();

        if (row.HasContent)
        {
            _Unpicking = row;
            UnpickMessage = string.Format(AppResources.Intent_Unpick_Message_Format, row.Title);
            IsUnpickOpen = true;
            return;
        }

        await _Database.DeleteLogAsync(row.Log);
        await LoadLogsAsync();
        RefreshPickerLabel();
    }

    [RelayCommand]
    private void CloseUnpick()
    {
        _Unpicking = null;
        IsUnpickOpen = false;
    }

    [RelayCommand]
    private async Task ConfirmUnpickAsync()
    {
        var row = _Unpicking;
        _Unpicking = null;
        IsUnpickOpen = false;

        if (row is null)
            return;

        await _Database.DeleteLogAsync(row.Log);
        await LoadLogsAsync();

        foreach (var pick in PickerRows.Where(p => p.Intention.Id == row.Intention.Id))
            pick.IsSelected = false;

        RefreshPickerLabel();
    }

    [RelayCommand]
    private void NewIntention() => Draft.OpenNew();

    [RelayCommand]
    private void CancelDraft() => Draft.Close();

    /// <summary>A new intention named from inside the picker is taken as chosen for this day.</summary>
    [RelayCommand]
    private async Task SaveDraftAsync()
    {
        if (!Draft.Validate())
            return;

        var intention = new Intention();
        Draft.ApplyTo(intention);
        await _Database.SaveIntentionAsync(intention);

        await _Database.SaveLogAsync(new IntentionLog
        {
            IntentionId = intention.Id,
            EntryDate = _Entry.EntryDate.Date,
        });

        Draft.Close();
        await LoadLogsAsync();
        await OpenPickerAsync();
    }

    private void RefreshPickerLabel()
    {
        var picked = Logs.Count;
        PickerDoneLabel = picked switch
        {
            0 => AppResources.Intent_Picker_Done,
            1 => AppResources.Intent_Picker_Done_One,
            _ => string.Format(AppResources.Intent_Picker_Done_Format, picked),
        };
    }

    /// <summary>Footer action: commit the page, then hand off to the upload screen.</summary>
    [RelayCommand]
    private async Task UploadAsync()
    {
        await SaveAsync();
        await _Navigation.PushAsync(Routes.Upload);
    }

    [RelayCommand]
    private Task CloseAsync() => _Navigation.BackAsync();

    private static string FormatSavedAt(DateTime when) =>
        string.Format(AppResources.Saved_At_Format, when.ToString("t", CultureInfo.CurrentCulture));

    private static int CountWords(string text) =>
        text.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries).Length;
}
