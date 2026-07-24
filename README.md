# Journal App

An offline-first journal app built with .NET MAUI, create entries, they auto-save when you leave the editor, and you can optionally upload individual entries to a Notion database.

## Stack

- .NET 10, C#, .NET MAUI (MVVM via [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/))
- Local storage: SQLite ([sqlite-net-pcl](https://github.com/praeclarum/sqlite-net)), created automatically on first launch
- Targets: Android, iOS, Windows

## Project structure

```
JournalApp/
  Models/       JournalEntry (SQLite table)
  Data/         JournalDatabase — SQLite access
  Services/     NotionService, AppSettings (Preferences wrapper)
  ViewModels/   JournalListViewModel, JournalEditorViewModel
  Views/        JournalListPage (home), JournalEditorPage (full-screen editor)
  Constants.cs  Max journal length, Notion token/version
```

## Running

```bash
dotnet build -f net10.0-windows10.0.19041.0   # Windows
dotnet build -f net10.0-android               # Android
dotnet build -f net10.0-ios                   # iOS (requires macOS + paired Mac or Mac build host)
```

Or open in Visual Studio / VS Code and run with the target device selector.

## Notion integration

Entries can be uploaded one at a time via the ⋮ menu on the home screen. On first launch, the app automatically finds or creates a "Journal" database in your Notion workspace and caches its ID locally (via `Preferences`), so this only happens once.

### Setup

1. Create an internal integration at [notion.so/my-integrations](https://www.notion.so/my-integrations) and copy its secret.
2. Set an environment variable named `NOTIONTOKEN` to that secret.
   - **Windows**: `setx NOTIONTOKEN "secret_..."` (restart your terminal/IDE afterwards), or set it via System Properties → Environment Variables.
   - This only works for the Windows build — Android and iOS apps run in their own OS sandbox and cannot see your development machine's environment variables. To test Notion upload on mobile, you'd need to inject the token another way (e.g. build-time MSBuild property), which is out of scope for this MVP.
3. Run the app. If `NOTIONTOKEN` isn't set, Notion features are silently unavailable — local journaling still works fully offline.

### What gets created

A "Journal" database with exactly these properties:

| Property           | Type      |
| ------------------ | --------- |
| Day Number         | Number    |
| Uploaded Date Time | Date      |
| Journal Text       | Rich Text |

(Notion also requires a title property on every database; it's added automatically and populated with "Day N".)

Internal integration tokens can't create pages directly at the workspace root, but _can_ create databases there. The app first tries to create a wrapper page for the database; if that's rejected (typical for internal integrations), it falls back to creating the "Journal" database directly at the workspace level. Either way, no manual page-sharing step is required.

After a successful upload, the entry is marked as uploaded locally (✓ Notion badge) to avoid accidental duplicates; re-uploading requires explicit confirmation.

## Notes

- Max entry length is 10,000 characters (`Constants.MaxJournalLength`).
- There's no Save button — edits are written to SQLite when you navigate away from the editor (`JournalEditorPage.OnDisappearing`).
- The Notion API version in use is `2026-03-11` (current at time of writing), which uses the "data source" model — databases and their schemas are separate objects as of the `2025-09-03` API version.
