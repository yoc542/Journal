using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Models;
using JournalApp.Services;
using JournalApp.Views;

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
    private readonly NotionService _Notion;

    private List<JournalEntry> _All = new();

    [ObservableProperty] private ObservableCollection<MonthGroup> _Months = new();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _IsBusy;
    [ObservableProperty] private string _NoResultsMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _HasEntries;

    /// <summary>Entries exist but the current search excluded all of them.</summary>
    [ObservableProperty] private bool _HasNoResults;

    [ObservableProperty] private string _Query = string.Empty;

    public JournalListViewModel(JournalDatabase database, NotionService notion)
    {
        _Database = database;
        _Notion = notion;
    }

    /// <summary>Nothing has ever been written, so the search box and list are pointless.</summary>
    public bool IsEmpty => !HasEntries;

    public bool IsNotBusy => !IsBusy;

    public bool IsImportVisible => Constants.DeveloperMode;

    partial void OnQueryChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadAsync()
    {
        _All = await _Database.GetEntriesAsync();
        HasEntries = _All.Count > 0;
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
    private static Task OpenEntryAsync(JournalEntry entry) =>
        Shell.Current.GoToAsync($"{nameof(EntryDetailPage)}?id={entry.Id}");

    [RelayCommand]
    private static Task WriteTodayAsync() => Shell.Current.GoToAsync(nameof(JournalEditorPage));

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task ImportFromNotionAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            var imported = await _Notion.FetchEntriesAsync();
            foreach (var entry in imported)
            {
                entry.IsUploaded = true;
                await _Database.SaveEntryAsync(entry);
            }
            await LoadAsync();
            await Shell.Current.DisplayAlertAsync(
                AppResources.Import_SuccessTitle,
                string.Format(AppResources.Import_SuccessMessage_Format, imported.Count),
                AppResources.OK);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(AppResources.Import_FailTitle, ex.Message, AppResources.OK);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
