using JournalApp.Services;
using JournalApp.Views;

namespace JournalApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(JournalEditorPage), typeof(JournalEditorPage));
        Routing.RegisterRoute(nameof(JournalListPage), typeof(JournalListPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

        // Onboarding is the first ShellContent, so returning users skip straight to their journal.
        if (AppSettings.SetupCompleted)
            CurrentItem = TodayShell;
    }
}
