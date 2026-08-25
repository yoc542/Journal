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
}
