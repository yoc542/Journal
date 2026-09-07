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
        builder.Services.AddSingleton<NavigationService>();
        builder.Services.AddSingleton(sp => new NotionService(new HttpClient()));

        // ViewModels
        builder.Services.AddTransient<JournalListViewModel>();
        builder.Services.AddTransient<JournalEditorViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<OnboardingViewModel>();
        builder.Services.AddTransient<TodayViewModel>();
        builder.Services.AddTransient<EntryDetailViewModel>();
        builder.Services.AddTransient<NotionConnectViewModel>();
        builder.Services.AddTransient<UploadViewModel>();
        builder.Services.AddTransient<ImportViewModel>();
        builder.Services.AddTransient<PinViewModel>();
        builder.Services.AddTransient<LockViewModel>();
        builder.Services.AddTransient<IntentionsViewModel>();

        // Views
        builder.Services.AddTransient<JournalListPage>();
        builder.Services.AddTransient<JournalEditorPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<TodayPage>();
        builder.Services.AddTransient<EntryDetailPage>();
        builder.Services.AddTransient<NotionConnectPage>();
        builder.Services.AddTransient<UploadPage>();
        builder.Services.AddTransient<ImportPage>();
        builder.Services.AddTransient<PinPage>();
        builder.Services.AddTransient<LockPage>();
        builder.Services.AddTransient<IntentionsPage>();

        RemoveNativeInputBorder();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }


    /// <summary>Both Entry and Editor draw their own native box, which doubles up with the
    /// MAUI Border the field styles wrap them in.</summary>
    private static void RemoveNativeInputBorder()
    {
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoNativeBorder", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.Background = null;
#elif IOS || MACCATALYST
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif WINDOWS
            StripNativeBox(handler.PlatformView);
#endif
        });

        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoNativeBorder", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.Background = null;
#elif WINDOWS
            StripNativeBox(handler.PlatformView);
#endif
        });
    }

#if WINDOWS
    private static void StripNativeBox(Microsoft.UI.Xaml.Controls.TextBox view)
    {
        var none = new Microsoft.UI.Xaml.Thickness(0);
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        view.BorderThickness = none;

        view.Resources["TextControlBorderThemeThickness"] = none;
        view.Resources["TextControlBorderThemeThicknessFocused"] = none;
        view.Resources["TextControlBackground"] = transparent;
        view.Resources["TextControlBackgroundPointerOver"] = transparent;
        view.Resources["TextControlBackgroundFocused"] = transparent;
    }
#endif
}
