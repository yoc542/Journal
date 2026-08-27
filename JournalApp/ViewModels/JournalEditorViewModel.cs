using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Models;
using JournalApp.Views;

namespace JournalApp.ViewModels;

public partial class JournalEditorViewModel : ObservableObject
{
    /// <summary>Quiet period after the last keystroke before the page is written to disk.</summary>
    private const int AutoSaveDelayMs = 900;

    private readonly JournalDatabase _Database;

    private JournalEntry _Entry = new();
    private CancellationTokenSource? _AutoSave;
    private bool _IsLoading;

    [ObservableProperty] private int _EntryId;
    [ObservableProperty] private string _Text = string.Empty;
    [ObservableProperty] private string _DateLabel = string.Empty;
    [ObservableProperty] private string _HeaderTitle = string.Empty;
    [ObservableProperty] private string _BackLabel = string.Empty;
    [ObservableProperty] private string _SaveStatus = string.Empty;
    [ObservableProperty] private bool _IsSaving;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WordCountLabel))]
    private int _WordCount;

    public bool IsNotBusy => !IsBusy;

    public int MaxLength => Constants.MaxJournalLength;
    public string PlaceholderText => AppResources.Editor_Placeholder;

    public string WordCountLabel => WordCount == 1
        ? AppResources.Editor_WordCount_One
        : string.Format(AppResources.Editor_WordCount_Format, WordCount);

    public JournalEditorViewModel(JournalDatabase database) => _Database = database;

    partial void OnTextChanged(string value)
    {
        if (_IsLoading)
            return;

        WordCount = CountWords(value ?? string.Empty);
        IsSaving = true;
        SaveStatus = AppResources.Editor_Saving;
        QueueAutoSave();
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
    }

    /// <summary>Debounced autosave — the newest keystroke supersedes any pending write.</summary>
    private void QueueAutoSave()
    {
        _AutoSave?.Cancel();
        var cts = new CancellationTokenSource();
        _AutoSave = cts;
        _ = AutoSaveAsync(cts.Token);
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

        // Nothing worth a row yet: a brand-new page the user has not written in.
        if (_Entry.Id == 0 && text.Length == 0)
        {
            IsSaving = false;
            SaveStatus = AppResources.Saved_Never;
            return;
        }

        if (_Entry.Text != text)
        {
            _Entry.Text = text;
            _Entry.IsUploaded = false; // content changed since the last upload
            await _Database.SaveEntryAsync(_Entry);
            EntryId = _Entry.Id;
        }

        IsSaving = false;
        SaveStatus = FormatSavedAt(_Entry.UpdatedAt);
    }

    /// <summary>Footer action: commit the page, then hand off to the upload screen.</summary>
    [RelayCommand]
    private async Task UploadAsync()
    {
        await SaveAsync();
        await Shell.Current.GoToAsync(nameof(UploadPage));
    }

    [RelayCommand]
    private static Task CloseAsync() => Shell.Current.GoToAsync("..");

    private static string FormatSavedAt(DateTime when) =>
        string.Format(AppResources.Saved_At_Format, when.ToString("t", CultureInfo.CurrentCulture));

    private static int CountWords(string text) =>
        text.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries).Length;
}
