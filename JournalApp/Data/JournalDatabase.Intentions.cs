using JournalApp.Models;

namespace JournalApp.Data;

public partial class JournalDatabase
{

    public async Task<List<Intention>> GetIntentionsAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Intention>().OrderBy(i => i.Id).ToListAsync();
    }

    public async Task<List<Intention>> GetActiveIntentionsAsync()
    {
        var all = await GetIntentionsAsync();
        return all.Where(i => i.IsActive).ToList();
    }

    public async Task<Intention?> GetIntentionAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Intention>().FirstOrDefaultAsync(i => i.Id == id);
    }

    /// <summary>Looks an intention up by its stable public identifier, used when importing from Notion.</summary>
    public async Task<Intention?> GetIntentionByUidAsync(string uid)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Intention>().FirstOrDefaultAsync(i => i.Uid == uid);
    }

    public async Task<int> SaveIntentionAsync(Intention intention)
    {
        var db = await GetConnectionAsync();

        if (intention.Id != 0)
        {
            await db.UpdateAsync(intention);
            return intention.Id;
        }

        if (string.IsNullOrEmpty(intention.Uid))
            intention.Uid = await NewUniqueUidAsync();

        await db.InsertAsync(intention);
        return intention.Id;
    }

    public async Task DeleteIntentionAsync(Intention intention)
    {
        var db = await GetConnectionAsync();
        await db.Table<IntentionLog>().Where(l => l.IntentionId == intention.Id).DeleteAsync();
        await db.DeleteAsync(intention);
    }

    public async Task<Dictionary<int, int>> GetLoggedDayCountsAsync()
    {
        var db = await GetConnectionAsync();
        var logs = await db.Table<IntentionLog>().ToListAsync();
        return logs
            .Where(l => l.HasContent)
            .GroupBy(l => l.IntentionId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>The intentions picked for one day, in the order they were picked.</summary>
    public async Task<List<IntentionLog>> GetLogsForDateAsync(DateTime date)
    {
        var db = await GetConnectionAsync();
        var day = date.Date;
        return await db.Table<IntentionLog>()
            .Where(l => l.EntryDate == day)
            .OrderBy(l => l.Id)
            .ToListAsync();
    }

    /// <summary>Every log ever written, used to count and to render the Notion payload.</summary>
    public async Task<List<IntentionLog>> GetLogsAsync(int intentionId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<IntentionLog>()
            .Where(l => l.IntentionId == intentionId)
            .OrderByDescending(l => l.EntryDate)
            .ToListAsync();
    }

    public async Task<int> SaveLogAsync(IntentionLog log)
    {
        var db = await GetConnectionAsync();
        log.UpdatedAt = DateTime.Now;

        if (log.Id != 0)
        {
            await db.UpdateAsync(log);
            return log.Id;
        }

        await db.InsertAsync(log);
        return log.Id;
    }

    public async Task DeleteLogAsync(IntentionLog log)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(log);
    }

    /// <summary>One line per day that has anything logged against it, for the history list to show
    /// on days that were logged but never written about.</summary>
    public async Task<Dictionary<DateTime, string>> GetLogPreviewsAsync(int max)
    {
        var db = await GetConnectionAsync();
        var logs = await db.Table<IntentionLog>().OrderBy(l => l.Id).ToListAsync();
        var previews = new Dictionary<DateTime, string>();

        foreach (var log in logs.Where(l => l.HasContent))
        {
            var preview = log.SummarizeDid(max);
            if (preview.Length > 0 && !previews.ContainsKey(log.EntryDate.Date))
                previews[log.EntryDate.Date] = preview;
        }

        return previews;
    }

    /// <summary>What the user wanted on one day, flattened for Notion. Days that were only
    /// picked, never written about, carry nothing worth uploading.</summary>
    public async Task<List<IntentionLine>> GetIntentionLinesAsync(DateTime date)
    {
        var logs = await GetLogsForDateAsync(date);
        var intentions = await GetIntentionsAsync();

        return logs
            .Where(l => l.HasContent)
            .Join(intentions, l => l.IntentionId, i => i.Id,
                (log, intention) => new IntentionLine(intention.Uid, intention.Title, log.Did, log.Evidence))
            .ToList();
    }

    /// <summary>Rewrites one day's logs from what Notion holds.</summary>
    public async Task ReplaceLogsForDateAsync(DateTime date, IEnumerable<IntentionLine> lines)
    {
        var db = await GetConnectionAsync();
        var day = date.Date;

        await db.Table<IntentionLog>().Where(l => l.EntryDate == day).DeleteAsync();

        foreach (var line in lines)
        {
            var intention = await ResolveIntentionAsync(line);
            await SaveLogAsync(new IntentionLog
            {
                IntentionId = intention.Id,
                EntryDate = day,
                Did = line.Did,
                Evidence = line.Evidence,
            });
        }
    }

    /// <summary>The local intention a Notion line refers to. The stable identifier decides it, so a
    /// renamed intention still matches; the title is only a fallback for rows written without one.</summary>
    private async Task<Intention> ResolveIntentionAsync(IntentionLine line)
    {
        if (line.Uid.Length > 0 && await GetIntentionByUidAsync(line.Uid) is { } known)
            return known;

        var all = await GetIntentionsAsync();
        var sameName = all.FirstOrDefault(i =>
            string.Equals(i.Title, line.Title, StringComparison.CurrentCultureIgnoreCase));

        if (sameName is not null)
            return sameName;

        var created = new Intention { Uid = line.Uid, Title = line.Title };
        await SaveIntentionAsync(created);
        return created;
    }

    /// <summary>Draws a new identifier, retrying in the vanishingly unlikely event of a collision.</summary>
    private async Task<string> NewUniqueUidAsync()
    {
        while (true)
        {
            var uid = Intention.NewUid();
            if (await GetIntentionByUidAsync(uid) is null)
                return uid;
        }
    }
}
