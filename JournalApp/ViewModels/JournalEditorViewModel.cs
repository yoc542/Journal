using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Models;
using JournalApp.Resources.Strings;
using JournalApp.Views;

namespace JournalApp.ViewModels;

public partial class JournalEditorViewModel : ObservableObject
{
    private readonly JournalDatabase _Database;
    private JournalEntry _Entry = new();

    [ObservableProperty] private int _EntryId;
    [ObservableProperty] private string _Text = string.Empty;
    [ObservableProperty] private int _CharacterCount;
    [ObservableProperty] private string _HeaderTitle = string.Empty;
    [ObservableProperty] private string _HeaderSubtitle = string.Empty;

    public int MaxLength => Constants.MaxJournalLength;
    public string PlaceholderText => AppResources.Editor_Placeholder;

    public JournalEditorViewModel(JournalDatabase database) => _Database = database;

    partial void OnTextChanged(string value) => CharacterCount = value?.Length ?? 0;
    partial void OnCharacterCountChanged(int value) => OnPropertyChanged(nameof(CharacterCountLabel));

    public string CharacterCountLabel => string.Format(AppResources.CharacterCount_Format, CharacterCount, MaxLength);

    public async Task LoadAsync()
    {
        _Entry = EntryId != 0
            ? await _Database.GetEntryAsync(EntryId) ?? new JournalEntry()
            : await _Database.GetEntryForDateAsync(DateTime.Today) ?? new JournalEntry();

        EntryId = _Entry.Id;
        Text = _Entry.Text;

        var isToday = _Entry.EntryDate.Date == DateTime.Today;
        var formattedDate = _Entry.EntryDate.ToString("dddd, MMMM d, yyyy", CultureInfo.CurrentCulture);

        HeaderTitle = isToday ? AppResources.Today_Title : formattedDate;
        HeaderSubtitle = isToday ? formattedDate : string.Format(AppResources.Entry_Day_Format, _Entry.DayNumber);
    }

    /// <summary>Notepad-style auto-save: called when the editor page disappears.</summary>
    public async Task SaveAsync()
    {
        var text = (Text ?? string.Empty).Trim();

        // Don't persist an empty, never-saved entry.
        if (_Entry.Id == 0 && text.Length == 0)
            return;

        if (_Entry.Text == text)
            return;

        _Entry.Text = text;
        _Entry.IsUploaded = false; // content changed since last upload
        await _Database.SaveEntryAsync(_Entry);
        EntryId = _Entry.Id;
    }

    [RelayCommand]
    private static Task OpenHistoryAsync() => Shell.Current.GoToAsync(nameof(JournalListPage));
}
