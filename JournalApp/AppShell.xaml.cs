using JournalApp.Services;
using JournalApp.Views;

namespace JournalApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.JournalEditor, typeof(JournalEditorPage));
        Routing.RegisterRoute(Routes.JournalList, typeof(JournalListPage));
        Routing.RegisterRoute(Routes.EntryDetail, typeof(EntryDetailPage));
        Routing.RegisterRoute(Routes.NotionConnect, typeof(NotionConnectPage));
        Routing.RegisterRoute(Routes.Upload, typeof(UploadPage));
        Routing.RegisterRoute(Routes.Import, typeof(ImportPage));
        Routing.RegisterRoute(Routes.Settings, typeof(SettingsPage));
        Routing.RegisterRoute(Routes.Pin, typeof(PinPage));
        Routing.RegisterRoute(Routes.Intentions, typeof(IntentionsPage));

        // First launch walks the wizard; afterwards the PIN, when there is one, guards the journal.
        CurrentItem = !AppSettings.SetupCompleted ? OnboardingShell
            : AppSettings.PinSet ? LockShell
            : TodayShell;
    }
}
