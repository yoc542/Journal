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
        Routing.RegisterRoute(nameof(EntryDetailPage), typeof(EntryDetailPage));
        Routing.RegisterRoute(nameof(NotionConnectPage), typeof(NotionConnectPage));
        Routing.RegisterRoute(nameof(UploadPage), typeof(UploadPage));
        Routing.RegisterRoute(nameof(ImportPage), typeof(ImportPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(PinPage), typeof(PinPage));

        // First launch walks the wizard; afterwards the PIN, when there is one, guards the journal.
        CurrentItem = !AppSettings.SetupCompleted ? OnboardingShell
            : AppSettings.PinSet ? LockShell
            : TodayShell;
    }
}
