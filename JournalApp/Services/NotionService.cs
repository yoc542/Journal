using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using JournalApp.Models;
using JournalApp.Localization;

namespace JournalApp.Services;

/// <summary>Minimal Notion REST client for the "Journal" database (2026-03-11 data-source model).</summary>
public class NotionService
{
    private const string TitleProperty = "Name"; // Notion requires exactly one title property.
    private const string EntryDateProperty = "Entry Date";
    private const string JournalTitle = "Journal";

    private readonly HttpClient _Http;
    private readonly SemaphoreSlim _SetupGate = new(1, 1);

    public NotionService(HttpClient http)
    {
        _Http = http;
        _Http.BaseAddress = new Uri("https://api.notion.com/v1/");
        _Http.DefaultRequestHeaders.Add("Notion-Version", Constants.NotionVersion);
    }

    /// <summary>Verifies a token with Notion before it is saved, so a typo surfaces on the settings page.</summary>
    public async Task<bool> IsTokenValidAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _Http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Whether a token has been stored (or supplied through the environment).</summary>
    public static async Task<bool> IsConnectedAsync() =>
        !string.IsNullOrWhiteSpace(await SecureSettings.GetNotionTokenAsync());

    /// <summary>Validates a token, stores it, and makes sure the Journal database exists.
    /// Returns false when Notion rejects the token, in which case nothing is saved.</summary>
    public async Task<bool> ConnectAsync(string token)
    {
        if (!await IsTokenValidAsync(token))
            return false;

        await SecureSettings.SetNotionTokenAsync(token);
        await EnsureJournalDatabaseAsync();
        return true;
    }

    /// <summary>
    /// First-launch setup: reuse the cached data source, else find the "Journal" database in the
    /// workspace, else create a workspace-level page and a "Journal" database underneath it.
    /// </summary>
    public async Task EnsureJournalDatabaseAsync()
    {
        if (IsSetupComplete)
            return;

        await AuthorizeAsync();

        // Serialized so a concurrent startup and upload can't create two databases.
        await _SetupGate.WaitAsync();
        try
        {
            if (IsSetupComplete)
                return;

            var existing = await FindJournalDataSourceAsync();
            if (existing is not null)
            {
                CacheJournalIds(existing.Value.DatabaseId, existing.Value.DataSourceId);
                return;
            }

            var parentPageId = await EnsureJournalPageAsync();
            var databaseId = await CreateJournalDatabaseAsync(parentPageId);
            CacheJournalIds(databaseId, await GetFirstDataSourceIdAsync(databaseId));
            AppSettings.NotionEntryDateReady = true;
        }
        finally
        {
            _SetupGate.Release();
        }
    }

    /// <summary>
    /// Uploads an entry. If it was already uploaded (<see cref="JournalEntry.NotionPageId"/> is set), the existing
    /// row is updated in place instead of creating a duplicate. Returns the page ID (new or existing).
    /// </summary>
    public async Task<string> UploadEntryAsync(JournalEntry entry)
    {
        await AuthorizeAsync();
        await EnsureEntryDatePropertyAsync();

        var properties = new JsonObject
        {
            [TitleProperty] = TitleValue($"Day {entry.DayNumber}"),
            ["Day Number"] = new JsonObject { ["number"] = entry.DayNumber },
            [EntryDateProperty] = new JsonObject
            {
                ["date"] = new JsonObject { ["start"] = entry.EntryDate.ToString("yyyy-MM-dd") }
            },
            ["Uploaded Date Time"] = new JsonObject
            {
                ["date"] = new JsonObject { ["start"] = DateTime.Now.ToString("o") }
            },
            ["Journal Text"] = new JsonObject { ["rich_text"] = RichText(entry.Text) }
        };

        if (!string.IsNullOrEmpty(entry.NotionPageId))
        {
            await PatchJsonAsync($"pages/{entry.NotionPageId}", new JsonObject { ["properties"] = properties });
            return entry.NotionPageId;
        }

        var payload = new JsonObject
        {
            ["parent"] = new JsonObject
            {
                ["type"] = "data_source_id",
                ["data_source_id"] = await RequireDataSourceIdAsync()
            },
            ["properties"] = properties
        };

        var json = await PostJsonAsync("pages", payload);
        return json?["id"]?.GetValue<string>() ?? string.Empty;
    }

    /// <summary>Fetches every row from the Notion "Journal" database.</summary>
    public async Task<List<JournalEntry>> FetchEntriesAsync()
    {
        await AuthorizeAsync();

        var dataSourceId = await RequireDataSourceIdAsync();
        var json = await PostJsonAsync($"data_sources/{dataSourceId}/query", new JsonObject());

        var entries = new List<JournalEntry>();
        foreach (var page in json?["results"]?.AsArray() ?? new JsonArray())
        {
            var props = page?["properties"];
            var text = string.Concat((props?["Journal Text"]?["rich_text"]?.AsArray() ?? [])
                .Select(t => t?["plain_text"]?.GetValue<string>() ?? string.Empty));

            if (string.IsNullOrEmpty(text))
                continue;

            entries.Add(new JournalEntry
            {
                DayNumber = (int)(props?["Day Number"]?["number"]?.GetValue<double>() ?? 0),
                EntryDate = ReadEntryDate(props),
                Text = text,
                NotionPageId = page?["id"]?.GetValue<string>()
            });
        }
        return entries;
    }

    /// <summary>
    /// Which calendar day a Notion row belongs to. Rows written before "Entry Date" existed fall
    /// back to the upload timestamp's date, and finally to today, so an old database still imports.
    /// </summary>
    private static DateTime ReadEntryDate(JsonNode? props)
    {
        var candidates = new[]
        {
            props?[EntryDateProperty]?["date"]?["start"]?.GetValue<string>(),
            props?["Uploaded Date Time"]?["date"]?["start"]?.GetValue<string>(),
        };

        foreach (var value in candidates)
            if (DateTime.TryParse(value, out var parsed))
                return parsed.Date;

        return DateTime.Today;
    }

    /// <summary>
    /// Adds the "Entry Date" property to an existing Journal data source. Databases created by
    /// older versions of the app lack it, and Notion rejects writes to unknown properties.
    /// Notion merges the schema, so this is idempotent; the result is cached in preferences.
    /// </summary>
    private async Task EnsureEntryDatePropertyAsync()
    {
        if (AppSettings.NotionEntryDateReady)
            return;

        var dataSourceId = await RequireDataSourceIdAsync();
        await PatchJsonAsync($"data_sources/{dataSourceId}", new JsonObject
        {
            ["properties"] = new JsonObject
            {
                [EntryDateProperty] = new JsonObject { ["date"] = new JsonObject() }
            }
        });

        AppSettings.NotionEntryDateReady = true;
    }

    // --- first-launch setup ---

    private static bool IsSetupComplete => !string.IsNullOrEmpty(AppSettings.NotionDataSourceId);

    private static void CacheJournalIds(string databaseId, string dataSourceId)
    {
        AppSettings.NotionDatabaseId = databaseId;
        AppSettings.NotionDataSourceId = dataSourceId;
    }

    /// <summary>Locates an existing "Journal" data source so a re-installed app reuses it instead of creating a second one.</summary>
    private async Task<(string DatabaseId, string DataSourceId)?> FindJournalDataSourceAsync()
    {
        var payload = new JsonObject
        {
            ["query"] = JournalTitle,
            ["filter"] = new JsonObject { ["property"] = "object", ["value"] = "data_source" }
        };

        var json = await PostJsonAsync("search", payload);

        foreach (var item in json?["results"]?.AsArray() ?? new JsonArray())
        {
            if (item?["in_trash"]?.GetValue<bool>() is true)
                continue;

            if (!string.Equals(DataSourceName(item), JournalTitle, StringComparison.OrdinalIgnoreCase))
                continue;

            // A data source's parent is the database that owns it.
            var dataSourceId = item?["id"]?.GetValue<string>();
            var databaseId = item?["parent"]?["database_id"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(dataSourceId) && !string.IsNullOrEmpty(databaseId))
                return (databaseId, dataSourceId);
        }
        return null;
    }

    private static string? DataSourceName(JsonNode? dataSource) =>
        dataSource?["title"]?.AsArray().FirstOrDefault()?["plain_text"]?.GetValue<string>()
        ?? dataSource?["name"]?.GetValue<string>();

    /// <summary>
    /// Returns the workspace-level page that hosts the database, creating it on first launch.
    /// Public connections and personal access tokens create a workspace page by passing
    /// <c>"parent": { "workspace": true }</c>; a database itself always needs a page parent.
    /// </summary>
    private async Task<string> EnsureJournalPageAsync()
    {
        if (AppSettings.NotionParentPageId is { Length: > 0 } cached)
            return cached;

        var payload = new JsonObject
        {
            ["parent"] = new JsonObject { ["workspace"] = true },
            ["properties"] = new JsonObject { ["title"] = TitleValue(JournalTitle) }
        };

        var json = await PostJsonAsync("pages", payload);
        var pageId = json?["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Notion did not return a page ID.");

        AppSettings.NotionParentPageId = pageId;
        return pageId;
    }

    private async Task<string> CreateJournalDatabaseAsync(string parentPageId)
    {
        var properties = new JsonObject
        {
            [TitleProperty] = new JsonObject { ["title"] = new JsonObject() },
            ["Day Number"] = new JsonObject { ["number"] = new JsonObject() },
            ["Uploaded Date Time"] = new JsonObject { ["date"] = new JsonObject() },
            [EntryDateProperty] = new JsonObject { ["date"] = new JsonObject() },
            ["Journal Text"] = new JsonObject { ["rich_text"] = new JsonObject() }
        };

        var payload = new JsonObject
        {
            ["parent"] = new JsonObject { ["type"] = "page_id", ["page_id"] = parentPageId },
            ["title"] = TitleText(JournalTitle),
            ["initial_data_source"] = new JsonObject { ["properties"] = properties }
        };

        var json = await PostJsonAsync("databases", payload);
        return json?["id"]?.GetValue<string>()
               ?? throw new InvalidOperationException("Notion did not return a database ID.");
    }

    private async Task<string> GetFirstDataSourceIdAsync(string databaseId)
    {
        var json = await GetJsonAsync($"databases/{databaseId}");
        return json?["data_sources"]?.AsArray().FirstOrDefault()?["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Notion database has no data source.");
    }

    /// <summary>Applies the stored token to the client, throwing if the user has not connected Notion yet.</summary>
    private async Task AuthorizeAsync()
    {
        var token = await SecureSettings.GetNotionTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(AppResources.Notion_NotConnected);

        _Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<string> RequireDataSourceIdAsync()
    {
        await EnsureJournalDatabaseAsync();
        return AppSettings.NotionDataSourceId is { Length: > 0 } dataSourceId
            ? dataSourceId
            : throw new InvalidOperationException("Could not resolve the Notion Journal data source.");
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

    // --- transport helpers ---

    private Task<JsonObject?> GetJsonAsync(string url) => ReadJsonAsync(_Http.GetAsync(url));

    private Task<JsonObject?> PostJsonAsync(string url, JsonObject body) =>
        ReadJsonAsync(_Http.PostAsJsonAsync(url, body));

    private Task<JsonObject?> PatchJsonAsync(string url, JsonObject body) =>
        ReadJsonAsync(_Http.PatchAsJsonAsync(url, body));

    private static async Task<JsonObject?> ReadJsonAsync(Task<HttpResponseMessage> send)
    {
        using var response = await send;
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Notion API {(int)response.StatusCode}: {body}");
        }
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }
}
