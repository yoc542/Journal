using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Services;
using JournalApp.Views;

namespace JournalApp.ViewModels;

/// <summary>One cell of the Today screen's week strip.</summary>
public sealed record WeekDay(string Initial, string Mark, bool IsWritten);

public partial class TodayViewModel : ObservableObject
{
    private const int PreviewLength = 110;
    private const int WeekLength = 7;
    private const int MorningEndsAt = 12;
    private const int AfternoonEndsAt = 17;

    private readonly JournalDatabase _Database;

    [ObservableProperty] private string _DateLabel = string.Empty;
    [ObservableProperty] private string _Greeting = string.Empty;
    [ObservableProperty] private string _SaveStatus = string.Empty;
    [ObservableProperty] private string _Preview = string.Empty;
    [ObservableProperty] private string _WordCountLabel = string.Empty;
    [ObservableProperty] private string _ContinueLabel = string.Empty;
    [ObservableProperty] private string _PendingLabel = string.Empty;
    [ObservableProperty] private string _HistoryCountLabel = string.Empty;
    [ObservableProperty] private ObservableCollection<WeekDay> _Week = new();

    public TodayViewModel(JournalDatabase database) => _Database = database;

    public async Task LoadAsync()
    {
        DateLabel = DateTime.Today.ToString("dddd, d MMMM", CultureInfo.CurrentCulture);
        Greeting = BuildGreeting(AppSettings.UserName);

        var entry = await _Database.GetEntryForDateAsync(DateTime.Today);
        var text = entry?.Text ?? string.Empty;
        var words = entry?.WordCount ?? 0;

        Preview = text.Length == 0 ? AppResources.Today_Preview_Empty : entry!.Summarize(PreviewLength);
        ContinueLabel = text.Length == 0 ? AppResources.Today_Start : AppResources.Today_Continue;

        WordCountLabel = words == 1
            ? AppResources.Today_WordCount_One
            : string.Format(AppResources.Today_WordCount_Format, words);

        SaveStatus = entry is null
            ? AppResources.Saved_Never
            : string.Format(AppResources.Saved_At_Format, entry.UpdatedAt.ToString("t", CultureInfo.CurrentCulture));

        var pending = await _Database.GetPendingUploadCountAsync();
        PendingLabel = pending switch
        {
            0 => AppResources.Today_Upload_None,
            1 => AppResources.Today_Upload_Pending_One,
            _ => string.Format(AppResources.Today_Upload_Pending_Format, pending),
        };

        var total = await _Database.GetEntryCountAsync();
        HistoryCountLabel = total == 1
            ? AppResources.Today_History_Count_One
            : string.Format(AppResources.Today_History_Count_Format, total);

        await LoadWeekAsync();
    }

    /// <summary>The seven days ending today, marked according to whether anything was written.</summary>
    private async Task LoadWeekAsync()
    {
        var start = DateTime.Today.AddDays(-(WeekLength - 1));
        var written = await _Database.GetWrittenDatesAsync(start, DateTime.Today);

        var days = new ObservableCollection<WeekDay>();
        for (var i = 0; i < WeekLength; i++)
        {
            var date = start.AddDays(i);
            var isWritten = written.Contains(date);
            var abbreviation = date.ToString("ddd", CultureInfo.CurrentCulture);
            days.Add(new WeekDay(
                abbreviation.Length > 0 ? abbreviation[..1] : string.Empty,
                isWritten ? "✦" : "·",
                isWritten));
        }

        Week = days;
    }

    private static string BuildGreeting(string name)
    {
        var hour = DateTime.Now.Hour;

        if (name.Length == 0)
            return hour < MorningEndsAt ? AppResources.Today_Greeting_Morning
                : hour < AfternoonEndsAt ? AppResources.Today_Greeting_Afternoon
                : AppResources.Today_Greeting_Evening;

        var format = hour < MorningEndsAt ? AppResources.Today_Greeting_Morning_Format
            : hour < AfternoonEndsAt ? AppResources.Today_Greeting_Afternoon_Format
            : AppResources.Today_Greeting_Evening_Format;

        return string.Format(format, name);
    }

    [RelayCommand]
    private static Task ContinueWritingAsync() => Shell.Current.GoToAsync(nameof(JournalEditorPage));

    [RelayCommand]
    private static Task OpenHistoryAsync() => Shell.Current.GoToAsync(nameof(JournalListPage));

    [RelayCommand]
    private static Task OpenUploadAsync() => Shell.Current.GoToAsync(nameof(UploadPage));

    [RelayCommand]
    private static Task OpenSettingsAsync() => Shell.Current.GoToAsync(nameof(SettingsPage));
}
