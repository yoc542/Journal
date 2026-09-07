namespace JournalApp.Models;

/// <summary>One intention as it travels to and from Notion: the stable identifier that survives a
/// rename on either side, the title as it read at the time, and the day's two fields.</summary>
public sealed record IntentionLine(string Uid, string Title, string Did, string Evidence);
