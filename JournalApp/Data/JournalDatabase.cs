using JournalApp.Models;
using SQLite;

namespace JournalApp.Data;

/// <summary>Local SQLite store. The database file is created lazily on first access.</summary>
public class JournalDatabase
{
    private SQLiteAsyncConnection? _Connection;

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_Connection is not null)
            return _Connection;

        var path = Path.Combine(FileSystem.AppDataDirectory, "journal.db3");
        _Connection = new SQLiteAsyncConnection(path);
        await _Connection.CreateTableAsync<JournalEntry>();
        return _Connection;
    }

    public async Task<List<JournalEntry>> GetEntriesAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<JournalEntry>().OrderByDescending(e => e.UpdatedAt).ToListAsync();
    }

    public async Task<JournalEntry?> GetEntryAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<JournalEntry>().FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>Returns the single entry for the given calendar day, if one exists yet.</summary>
    public async Task<JournalEntry?> GetEntryForDateAsync(DateTime date)
    {
        var db = await GetConnectionAsync();
        var day = date.Date;
        return await db.Table<JournalEntry>().FirstOrDefaultAsync(e => e.EntryDate == day);
    }

    /// <summary>How many entries the journal holds, for the Today screen's history tile.</summary>
    public async Task<int> GetEntryCountAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<JournalEntry>().CountAsync();
    }

    /// <summary>Entries not yet pushed to Notion, oldest first, as the upload queue.</summary>
    public async Task<List<JournalEntry>> GetPendingEntriesAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<JournalEntry>()
            .Where(e => !e.IsUploaded)
            .OrderBy(e => e.EntryDate)
            .ToListAsync();
    }

    /// <summary>How many entries have not reached Notion yet.</summary>
    public async Task<int> GetPendingUploadCountAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<JournalEntry>().Where(e => !e.IsUploaded).CountAsync();
    }

    /// <summary>The days in the given inclusive range that already have an entry, for the week strip.</summary>
    public async Task<HashSet<DateTime>> GetWrittenDatesAsync(DateTime from, DateTime to)
    {
        var db = await GetConnectionAsync();
        var start = from.Date;
        var end = to.Date;
        var entries = await db.Table<JournalEntry>()
            .Where(e => e.EntryDate >= start && e.EntryDate <= end)
            .ToListAsync();
        return entries.Select(e => e.EntryDate.Date).ToHashSet();
    }

    public async Task<int> SaveEntryAsync(JournalEntry entry)
    {
        var db = await GetConnectionAsync();
        entry.UpdatedAt = DateTime.Now;

        if (entry.Id != 0)
        {
            await db.UpdateAsync(entry);
            return entry.Id;
        }

        var last = await db.Table<JournalEntry>().OrderByDescending(e => e.DayNumber).FirstOrDefaultAsync();
        entry.DayNumber = (last?.DayNumber ?? 0) + 1;
        await db.InsertAsync(entry);
        return entry.Id;
    }

    public async Task DeleteEntryAsync(JournalEntry entry)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(entry);
    }
}
