using CommunityToolkit.Mvvm.ComponentModel;
using JournalApp.Data;
using JournalApp.Models;

namespace JournalApp.ViewModels;

public partial class JournalEditorViewModel : ObservableObject
{
    private readonly JournalDatabase _Database;
    private JournalEntry _Entry = new();

    [ObservableProperty] private int _EntryId;
    [ObservableProperty] private string _Text = string.Empty;
    [ObservableProperty] private int _CharacterCount;

    public int MaxLength => Constants.MaxJournalLength;

    public JournalEditorViewModel(JournalDatabase database) => _Database = database;

    partial void OnTextChanged(string value) => CharacterCount = value?.Length ?? 0;

    public async Task LoadAsync()
    {
        _Entry = (EntryId != 0 ? await _Database.GetEntryAsync(EntryId) : null) ?? new JournalEntry();
        Text = _Entry.Text;
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
}
