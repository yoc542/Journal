using JournalApp.Services;

namespace JournalApp;

public partial class App : Application
{
    public App(NotionService notion)
    {
        InitializeComponent();

        // First-launch setup: create the Notion "Journal" database if needed.
        // No-op until a token is saved on the settings page.
        _ = Task.Run(async () =>
        {
            try { await notion.EnsureJournalDatabaseAsync(); }
            catch { /* offline or not connected yet — ignore, upload will surface errors */ }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
