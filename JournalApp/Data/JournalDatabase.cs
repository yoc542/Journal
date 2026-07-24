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
