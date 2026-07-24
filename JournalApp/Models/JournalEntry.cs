using SQLite;

namespace JournalApp.Models;

public class JournalEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Sequential, human-friendly number assigned on creation (survives deletes of other rows).</summary>
    public int DayNumber { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsUploaded { get; set; }

    /// <summary>First non-empty line, used as the list title.</summary>
    [Ignore]
    public string DisplayTitle
    {
        get
        {
            var line = Text.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
            return string.IsNullOrEmpty(line) ? "Untitled entry" : line;
        }
    }
}
