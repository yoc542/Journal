using System.Net.Http.Json;
using System.Text.Json.Nodes;
using JournalApp.Models;

namespace JournalApp.Services;

/// <summary>Minimal Notion REST client for the "Journal" database (2026-03-11 data-source model).</summary>
public class NotionService
{
    private const string TitleProperty = "Name"; // Notion requires exactly one title property.
    private readonly HttpClient _Http;

    public NotionService(HttpClient http)
    {
        _Http = http;
        _Http.BaseAddress = new Uri("https://api.notion.com/v1/");
        _Http.DefaultRequestHeaders.Add("Authorization", $"Bearer {Constants.NotionToken}");
        _Http.DefaultRequestHeaders.Add("Notion-Version", Constants.NotionVersion);
    }

    /// <summary>First-launch setup: reuse the cached data source, else find or create the "Journal" database.</summary>
    public async Task EnsureJournalDatabaseAsync()
    {
        if (!Constants.NotionConfigured || !string.IsNullOrEmpty(AppSettings.NotionDataSourceId))
            return;

        var found = await FindDataSourceAsync("Journal");
        var databaseId = found?.DatabaseId ?? await CreateJournalDatabaseAsync();
        AppSettings.NotionDatabaseId = databaseId;
        AppSettings.NotionDataSourceId = found?.DataSourceId ?? await GetDataSourceIdAsync(databaseId);
    }

    /// <summary>Uploads an entry as a new page/row. Returns the created page ID.</summary>
    public async Task<string> UploadEntryAsync(JournalEntry entry)
    {
        if (!Constants.NotionConfigured)
            throw new InvalidOperationException("Notion is not configured. Set the NOTIONTOKEN environment variable.");

        if (string.IsNullOrEmpty(AppSettings.NotionDataSourceId))
            await EnsureJournalDatabaseAsync();

        var dataSourceId = AppSettings.NotionDataSourceId
            ?? throw new InvalidOperationException("Could not resolve the Notion Journal data source.");

        var payload = new JsonObject
        {
            ["parent"] = new JsonObject { ["type"] = "data_source_id", ["data_source_id"] = dataSourceId },
            ["properties"] = new JsonObject
            {
                [TitleProperty] = TitleValue($"Day {entry.DayNumber}"),
                ["Day Number"] = new JsonObject { ["number"] = entry.DayNumber },
                ["Uploaded Date Time"] = new JsonObject
                {
                    ["date"] = new JsonObject { ["start"] = DateTime.Now.ToString("o") }
                },
                ["Journal Text"] = new JsonObject { ["rich_text"] = RichText(entry.Text) }
            }
        };

        using var response = await _Http.PostAsJsonAsync("pages", payload);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        return json?["id"]?.GetValue<string>() ?? string.Empty;
    }

    private async Task<(string DatabaseId, string DataSourceId)?> FindDataSourceAsync(string title)
    {
        var payload = new JsonObject
        {
            ["query"] = title,
            ["filter"] = new JsonObject { ["property"] = "object", ["value"] = "data_source", ["in_trash"] = false }
        };

        using var response = await _Http.PostAsJsonAsync("search", payload);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();

        foreach (var item in json?["results"]?.AsArray() ?? new JsonArray())
        {
            var name = item?["title"]?.AsArray().FirstOrDefault()?["plain_text"]?.GetValue<string>();
            if (!string.Equals(name, title, StringComparison.OrdinalIgnoreCase))
                continue;

            var dataSourceId = item?["id"]?.GetValue<string>();
            var databaseId = item?["parent"]?["database_id"]?.GetValue<string>();
            if (dataSourceId is not null && databaseId is not null)
                return (databaseId, dataSourceId);
        }
        return null;
    }

    private async Task<string> GetDataSourceIdAsync(string databaseId)
    {
        using var response = await _Http.GetAsync($"databases/{databaseId}");
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        return json?["data_sources"]?.AsArray().FirstOrDefault()?["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Notion database has no data source.");
    }

    private async Task<string> CreateJournalDatabaseAsync()
    {
        var properties = new JsonObject
        {
            [TitleProperty] = new JsonObject { ["title"] = new JsonObject() },
            ["Day Number"] = new JsonObject { ["number"] = new JsonObject() },
            ["Uploaded Date Time"] = new JsonObject { ["date"] = new JsonObject() },
            ["Journal Text"] = new JsonObject { ["rich_text"] = new JsonObject() }
        };

        // Internal integrations can't create workspace-level pages, so try a wrapper page
        // first and fall back to creating the database directly at the workspace level.
        var parentPageId = await TryCreateWrapperPageAsync();
        var parent = parentPageId is not null
            ? new JsonObject { ["type"] = "page_id", ["page_id"] = parentPageId }
            : new JsonObject { ["type"] = "workspace", ["workspace"] = true };

        var payload = new JsonObject
        {
            ["parent"] = parent,
            ["title"] = TitleText("Journal"),
            ["initial_data_source"] = new JsonObject { ["properties"] = properties }
        };

        using var response = await _Http.PostAsJsonAsync("databases", payload);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        return json?["id"]?.GetValue<string>()
               ?? throw new InvalidOperationException("Notion did not return a database ID.");
    }

    private async Task<string?> TryCreateWrapperPageAsync()
    {
        var payload = new JsonObject
        {
            ["parent"] = new JsonObject { ["type"] = "workspace", ["workspace"] = true },
            ["properties"] = new JsonObject { ["title"] = TitleValue("Journal") }
        };

        using var response = await _Http.PostAsJsonAsync("pages", payload);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        return json?["id"]?.GetValue<string>();
    }

    // --- payload helpers ---

    private static JsonObject TitleValue(string text) => new() { ["title"] = TitleText(text) };

    private static JsonArray TitleText(string text) =>
        new() { new JsonObject { ["type"] = "text", ["text"] = new JsonObject { ["content"] = text } } };

    /// <summary>Splits text into &lt;=2000-char chunks (Notion's per-rich-text limit).</summary>
    private static JsonArray RichText(string text)
    {
        var array = new JsonArray();
        for (var i = 0; i < text.Length; i += 2000)
        {
            var chunk = text.Substring(i, Math.Min(2000, text.Length - i));
            array.Add(new JsonObject { ["text"] = new JsonObject { ["content"] = chunk } });
        }
        return array;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;
        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"Notion API {(int)response.StatusCode}: {body}");
    }
}
