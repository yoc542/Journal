using System.Globalization;
using JournalApp.Localization;
using SQLite;

namespace JournalApp.Models;

public class JournalEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Sequential, human-friendly number assigned on creation (survives deletes of other rows).</summary>
    public int DayNumber { get; set; }

    /// <summary>The calendar day this entry belongs to (date-only). One entry per day.</summary>
    public DateTime EntryDate { get; set; } = DateTime.Today;

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsUploaded { get; set; }

    /// <summary>Notion page ID this entry was uploaded to, if any. Reused to update the same row instead of duplicating it.</summary>
    public string? NotionPageId { get; set; }

    /// <summary>First non-empty line, used as the list title.</summary>
    [Ignore]
    public string DisplayTitle
    {
        get
        {
            var line = Text.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
            return string.IsNullOrEmpty(line) ? AppResources.Untitled_Entry : line;
        }
    }

    /// <summary>Flattens the entry onto one line, clipped to <paramref name="max"/> characters.</summary>
    public string Summarize(int max)
    {
        var line = Text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return line.Length <= max ? line : line[..max].TrimEnd() + "\u2026";
    }

    /// <summary>One-line preview shown under the title in the history list.</summary>
    [Ignore]
    public string Excerpt => Summarize(ExcerptLength);

    /// <summary>Day of the month, shown in the history list's date column.</summary>
    [Ignore]
    public string DayLabel => EntryDate.Day.ToString(CultureInfo.CurrentCulture);

    /// <summary>Abbreviated weekday, shown under <see cref="DayLabel"/>.</summary>
    [Ignore]
    public string DayOfWeekLabel => EntryDate.ToString("ddd", CultureInfo.CurrentCulture);

    /// <summary>Whether this entry has reached Notion, as list-ready text.</summary>
    [Ignore]
    public string SyncLabel => IsUploaded
        ? AppResources.History_Sync_InNotion
        : AppResources.History_Sync_Pending;

    /// <summary>Whitespace-delimited word count of the saved text.</summary>
    [Ignore]
    public int WordCount => Text.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>Full date, used as the heading of the detail screen.</summary>
    [Ignore]
    public string FullDateLabel => EntryDate.ToString("dddd, d MMMM yyyy", CultureInfo.CurrentCulture);

    private const int ExcerptLength = 72;
}
