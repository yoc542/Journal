using JournalApp.Data;
using JournalApp.Services;
using JournalApp.ViewModels;
using JournalApp.Views;
using Microsoft.Extensions.Logging;

namespace JournalApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<JournalDatabase>();
        builder.Services.AddSingleton(sp => new NotionService(new HttpClient()));

        // ViewModels
        builder.Services.AddSingleton<JournalListViewModel>();
        builder.Services.AddTransient<JournalEditorViewModel>();

        // Views
        builder.Services.AddSingleton<JournalListPage>();
        builder.Services.AddTransient<JournalEditorPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
