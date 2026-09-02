# Journal App

An offline-first daily journal built with .NET MAUI. Write one entry a day, it saves itself,
and you can push entries to a Notion database whenever you want.

## Features

- **Today** — greeting, week strip, today's preview and word count, one tap to keep writing.
- **History** — every past entry with its date, excerpt and sync state; tap to read, edit or delete.
- **Notion sync** — connect an integration token in the app, then upload pending entries in bulk.
- **Notion import** — pull entries back from Notion with per-entry conflict resolution.

## Stack

.NET 10 · .NET MAUI · MVVM ([CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/))
· SQLite ([sqlite-net-pcl](https://github.com/praeclarum/sqlite-net)) · Android, iOS, Windows

## Project structure

```
JournalApp/
  Models/        JournalEntry (SQLite table)
  Data/          JournalDatabase — SQLite access
  Services/      NotionService, AppSettings (Preferences), SecureSettings (SecureStorage)
  ViewModels/    Onboarding, Today, JournalEditor, JournalList, EntryDetail, Upload, Import, Settings, NotionConnect
  Views/         Matching XAML pages
  Localization/  AppResources.resx — all user-facing strings
  Constants.cs   Max entry length, Notion API version
```

## Running

```bash
dotnet build -f net10.0-windows10.0.19041.0   # Windows
dotnet build -f net10.0-android               # Android
dotnet build -f net10.0-ios                   # iOS (requires a Mac build host)
```

Or open in Visual Studio / VS Code and pick a target from the device selector.

## Notion setup

1. Create an internal integration at [notion.so/my-integrations](https://www.notion.so/my-integrations) and copy its secret.
2. In the app, go to **Settings → Notion** and paste the token. It is stored in the platform
   keychain/keystore via `SecureStorage`.
3. On first upload the app finds or creates a "Journal" database in your workspace and caches its
   ID locally, so this happens only once.

The created database has `Day Number` (number), `Entry Date` / `Uploaded Date Time` (date) and
`Journal Text` (rich text) properties, plus the title Notion requires on every database.
