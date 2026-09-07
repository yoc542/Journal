using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Models;
using JournalApp.Services;

namespace JournalApp.ViewModels;

/// <summary>The entries of one calendar month, as a grouped-CollectionView section.</summary>
public class MonthGroup : List<JournalEntry>
{
    public MonthGroup(string label, IEnumerable<JournalEntry> entries) : base(entries) => Label = label;

    public string Label { get; }
}

public partial class JournalListViewModel : ObservableObject
{
    private readonly JournalDatabase _Database;
    private readonly NavigationService _Navigation;

    private const int PreviewLength = 72;

    private List<JournalEntry> _All = new();

    [ObservableProperty] private ObservableCollection<MonthGroup> _Months = new();
    [ObservableProperty] private string _NoResultsMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _HasEntries;

    /// <summary>Entries exist but the current search excluded all of them.</summary>
    [ObservableProperty] private bool _HasNoResults;

    [ObservableProperty] private string _Query = string.Empty;

    public JournalListViewModel(JournalDatabase database, NavigationService navigation)
    {
        _Database = database;
        _Navigation = navigation;
    }

    /// <summary>Nothing has ever been written, so the search box and list are pointless.</summary>
    public bool IsEmpty => !HasEntries;

    partial void OnQueryChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadAsync()
    {
        _All = await _Database.GetEntriesAsync();
        HasEntries = _All.Count > 0;

        // Days logged against an intention but never written about would otherwise read as blank.
        var previews = await _Database.GetLogPreviewsAsync(PreviewLength);
        foreach (var entry in _All.Where(e => e.Text.Trim().Length == 0))
            entry.LogFallback = previews.GetValueOrDefault(entry.EntryDate.Date, string.Empty);
        ApplyFilter();
    }

    /// <summary>Applies the search box and regroups what survives by month, newest first.</summary>
    private void ApplyFilter()
    {
        var query = (Query ?? string.Empty).Trim();

        var matches = query.Length == 0
            ? _All
            : _All.Where(e => Matches(e, query)).ToList();

        Months = new ObservableCollection<MonthGroup>(
            matches
                .OrderByDescending(e => e.EntryDate)
                .GroupBy(e => new DateTime(e.EntryDate.Year, e.EntryDate.Month, 1))
                .Select(g => new MonthGroup(
                    g.Key.ToString("MMMM yyyy", CultureInfo.CurrentCulture), g)));

        HasNoResults = HasEntries && matches.Count == 0;
        NoResultsMessage = string.Format(AppResources.History_NoResults_Format, query);
    }

    private static bool Matches(JournalEntry entry, string query) =>
        entry.Text.Contains(query, StringComparison.CurrentCultureIgnoreCase)
        || entry.FullDateLabel.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    [RelayCommand]
    private Task OpenEntryAsync(JournalEntry entry) =>
        _Navigation.PushAsync(Routes.EntryDetail, new Dictionary<string, object> { ["id"] = entry.Id });

    [RelayCommand]
    private Task WriteTodayAsync() => _Navigation.PushAsync(Routes.JournalEditor);

    [RelayCommand]
    private Task BackAsync() => _Navigation.BackAsync();

    [RelayCommand]
    private Task ImportAsync() => _Navigation.PushAsync(Routes.Import);
}
