using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Services;

namespace JournalApp.ViewModels;

/// <summary>One cell of the Today screen's week strip.</summary>
public sealed record WeekDay(int Index, string Initial, string Mark, bool IsWritten);

/// <summary>One intention picked for today, as the Today screen previews it.</summary>
/// <param name="StateLabel">Whether the day has been recorded, started, or only picked.</param>
/// <param name="Summary">What the user did, or a nudge to write it.</param>
public sealed record IntentionSummary(string Title, string StateLabel, string Summary, bool IsRecorded, bool IsUntouched);

public partial class TodayViewModel : ObservableObject
{
    private const int PreviewLength = 110;
    private const int WeekLength = 7;
    private const int SummaryLength = 76;
    private const int MorningEndsAt = 12;
    private const int AfternoonEndsAt = 17;

    private readonly JournalDatabase _Database;
    private readonly NavigationService _Navigation;

    [ObservableProperty] private string _DateLabel = string.Empty;
    [ObservableProperty] private string _Greeting = string.Empty;
    [ObservableProperty] private string _SaveStatus = string.Empty;
    [ObservableProperty] private string _Preview = string.Empty;
    [ObservableProperty] private string _WordCountLabel = string.Empty;
    [ObservableProperty] private string _ContinueLabel = string.Empty;
    [ObservableProperty] private string _PendingLabel = string.Empty;
    [ObservableProperty] private string _HistoryCountLabel = string.Empty;
    [ObservableProperty] private ObservableCollection<WeekDay> _Week = new();
    [ObservableProperty] private ObservableCollection<IntentionSummary> _Tonight = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoTonight))]
    private bool _HasTonight;

    public TodayViewModel(JournalDatabase database, NavigationService navigation)
    {
        _Database = database;
        _Navigation = navigation;
    }

    public bool HasNoTonight => !HasTonight;

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
        await LoadTonightAsync();
    }

    private async Task LoadTonightAsync()
    {
        var logs = await _Database.GetLogsForDateAsync(DateTime.Today);
        var intentions = await _Database.GetIntentionsAsync();

        Tonight = new ObservableCollection<IntentionSummary>(
            logs.Join(intentions, l => l.IntentionId, i => i.Id, (log, intention) =>
            {
                var summary = log.SummarizeDid(SummaryLength);

                return new IntentionSummary(
                    intention.Title,
                    log.IsComplete ? AppResources.Intent_State_Recorded
                        : log.HasContent ? AppResources.Intent_State_InProgress
                        : AppResources.Intent_State_Empty,
                    summary.Length > 0 ? summary : AppResources.Intent_Today_Prompt,
                    log.IsComplete,
                    !log.HasContent);
            }));

        HasTonight = Tonight.Count > 0;
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
                i,
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
    private Task ContinueWritingAsync() => _Navigation.PushAsync(Routes.JournalEditor);

    [RelayCommand]
    private Task OpenHistoryAsync() => _Navigation.PushAsync(Routes.JournalList);

    [RelayCommand]
    private Task OpenUploadAsync() => _Navigation.PushAsync(Routes.Upload);

    [RelayCommand]
    private Task OpenSettingsAsync() => _Navigation.PushAsync(Routes.Settings);

    [RelayCommand]
    private Task OpenIntentionsAsync() => _Navigation.PushAsync(Routes.Intentions);

    [RelayCommand]
    private Task ChooseIntentionsAsync() =>
        _Navigation.PushAsync(Routes.JournalEditor, new Dictionary<string, object> { ["picker"] = 1 });
}
