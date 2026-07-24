using JournalApp.Views;

namespace JournalApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(JournalEditorPage), typeof(JournalEditorPage));
    }
}
