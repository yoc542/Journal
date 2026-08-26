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
                fonts.AddFont("CormorantGaramond-Regular.ttf", "CormorantRegular");
                fonts.AddFont("CormorantGaramond-SemiBold.ttf", "CormorantSemiBold");
                fonts.AddFont("Karla-Regular.ttf", "KarlaRegular");
                fonts.AddFont("Karla-SemiBold.ttf", "KarlaSemiBold");
            });

        // Services
        builder.Services.AddSingleton<JournalDatabase>();
        builder.Services.AddSingleton(sp => new NotionService(new HttpClient()));

        // ViewModels
        builder.Services.AddTransient<JournalListViewModel>();
        builder.Services.AddTransient<JournalEditorViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<OnboardingViewModel>();
        builder.Services.AddTransient<TodayViewModel>();
        builder.Services.AddTransient<EntryDetailViewModel>();

        // Views
        builder.Services.AddTransient<JournalListPage>();
        builder.Services.AddTransient<JournalEditorPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<TodayPage>();
        builder.Services.AddTransient<EntryDetailPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
