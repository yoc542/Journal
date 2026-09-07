using System.Globalization;
using JournalApp.Localization;
using SQLite;

namespace JournalApp.Models;

public enum IntentionStatus
{
    Active = 0,
    Completed = 1,
}

public class Intention
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique]
    public string Uid { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public IntentionStatus Status { get; set; } = IntentionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }

    [Ignore]
    public bool IsActive => Status == IntentionStatus.Active;

    [Ignore]
    public string NoteLabel => string.IsNullOrWhiteSpace(Note) ? AppResources.Intent_NoDescription : Note;

    [Ignore]
    public string SinceLabel => CreatedAt.ToString("d MMMM", CultureInfo.CurrentCulture);

    [Ignore]
    public string CompletedLabel => (CompletedAt ?? CreatedAt).ToString("d MMMM", CultureInfo.CurrentCulture);

    public static string NewUid() => Guid.NewGuid().ToString("N")[..Constants.IntentionUidLength];
}
