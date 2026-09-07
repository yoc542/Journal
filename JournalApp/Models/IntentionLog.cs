using SQLite;

namespace JournalApp.Models;

public class IntentionLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int IntentionId { get; set; }

    [Indexed]
    public DateTime EntryDate { get; set; } = DateTime.Today;
    public string Did { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [Ignore]
    public bool HasContent => Did.Trim().Length > 0 || Evidence.Trim().Length > 0;

    [Ignore]
    public bool IsComplete => Did.Trim().Length > 0 && Evidence.Trim().Length > 0;

    public string SummarizeDid(int max)
    {
        var line = Did.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return line.Length <= max ? line : line[..max].TrimEnd() + "…";
    }
}
