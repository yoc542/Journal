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

public enum ImportStep { Scanning, Picker, Conflict, Done }

/// <summary>How a single date collision should be settled.</summary>
public enum ConflictChoice { Device, Incoming, Both }

/// <summary>A Notion page offered for import, with its local counterpart if one exists.</summary>
public partial class IncomingEntry : ObservableObject
{
    private const int PreviewLength = 90;

    public IncomingEntry(JournalEntry incoming, JournalEntry? local)
    {
        Incoming = incoming;
        Local = local;
        DateLabel = incoming.FullDateLabel;
        Title = incoming.DisplayTitle;
        FlagLabel = local is null ? AppResources.Import_Flag_New : AppResources.Import_Flag_Conflict;
        IncomingPreview = incoming.Summarize(PreviewLength);
        LocalPreview = local?.Summarize(PreviewLength) ?? string.Empty;
        IncomingWords = string.Format(AppResources.Conflict_Notion_Format, incoming.WordCount);
        LocalWords = string.Format(AppResources.Conflict_Device_Format, local?.WordCount ?? 0);
        _IsSelected = true;
    }

    public JournalEntry Incoming { get; }
    public JournalEntry? Local { get; }

    public bool IsConflict => Local is not null;

    public string DateLabel { get; }
    public string Title { get; }
    public string FlagLabel { get; }
    public string IncomingPreview { get; }
    public string LocalPreview { get; }
    public string IncomingWords { get; }
    public string LocalWords { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckMark))]
    private bool _IsSelected;

    public string CheckMark => IsSelected ? "✓" : string.Empty;
}

public partial class ImportViewModel : ObservableObject
{
    private readonly JournalDatabase _Database;
    private readonly NotionService _Notion;

    private List<IncomingEntry> _Conflicts = new();
    private readonly Dictionary<IncomingEntry, ConflictChoice> _Resolutions = new();
    private int _ConflictIndex;

    [ObservableProperty] private ObservableCollection<IncomingEntry> _Incoming = new();
    [ObservableProperty] private string _Heading = string.Empty;
    [ObservableProperty] private string _Subheading = string.Empty;
    [ObservableProperty] private string _ImportButtonLabel = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotConnected), nameof(IsScanning))]
    private bool _IsConnected = true;
    [ObservableProperty] private bool _ApplyToAll;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScanning), nameof(IsPicker), nameof(IsConflict), nameof(IsDone))]
    private ImportStep _Step = ImportStep.Scanning;

    // --- conflict screen ---
    [ObservableProperty] private string _ConflictEyebrow = string.Empty;
    [ObservableProperty] private string _ConflictTitle = string.Empty;
    [ObservableProperty] private string _LocalWords = string.Empty;
    [ObservableProperty] private string _LocalPreview = string.Empty;
    [ObservableProperty] private string _IncomingWords = string.Empty;
    [ObservableProperty] private string _IncomingPreview = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsKeepDevice), nameof(IsUseNotion), nameof(IsKeepBoth))]
    private ConflictChoice _Choice = ConflictChoice.Incoming;

    // --- done screen ---
    [ObservableProperty] private string _DoneTitle = string.Empty;
    [ObservableProperty] private string _DoneBody = string.Empty;

    public ImportViewModel(JournalDatabase database, NotionService notion)
    {
        _Database = database;
        _Notion = notion;
    }

    public bool IsNotConnected => !IsConnected;

    public bool IsScanning => IsConnected && Step == ImportStep.Scanning;
    public bool IsPicker => Step == ImportStep.Picker;
    public bool IsConflict => Step == ImportStep.Conflict;
    public bool IsDone => Step == ImportStep.Done;

    public bool IsKeepDevice => Choice == ConflictChoice.Device;
    public bool IsUseNotion => Choice == ConflictChoice.Incoming;
    public bool IsKeepBoth => Choice == ConflictChoice.Both;

    public string ApplyToAllMark => ApplyToAll ? "✓" : string.Empty;

    partial void OnApplyToAllChanged(bool value) => OnPropertyChanged(nameof(ApplyToAllMark));

    public async Task LoadAsync()
    {
        IsConnected = await NotionService.IsConnectedAsync();
        if (!IsConnected)
            return;

        Step = ImportStep.Scanning;
        Heading = AppResources.Import_Heading_Scanning;
        Subheading = string.Empty;

        try
        {
            var pages = await _Notion.FetchEntriesAsync();
            var local = await _Database.GetEntriesAsync();

            Incoming = new ObservableCollection<IncomingEntry>(
                pages
                    .OrderByDescending(p => p.EntryDate)
                    .Select(p => new IncomingEntry(
                        p, local.FirstOrDefault(l => l.EntryDate.Date == p.EntryDate.Date))));

            foreach (var item in Incoming)
                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(IncomingEntry.IsSelected))
                        RefreshPicker();
                };

            Step = ImportStep.Picker;
            RefreshPicker();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(AppResources.Import_FailTitle, ex.Message, AppResources.OK);
            await Shell.Current.GoToAsync("..");
        }
    }

    private void RefreshPicker()
    {
        var total = Incoming.Count;
        var conflicts = Incoming.Count(i => i.IsConflict);
        var selected = Incoming.Count(i => i.IsSelected);

        Heading = total switch
        {
            0 => AppResources.Import_Heading_None,
            1 => AppResources.Import_Heading_Found_One,
            _ => string.Format(AppResources.Import_Heading_Found_Format, total),
        };

        Subheading = total == 0
            ? AppResources.Import_Body_None
            : conflicts == 0
                ? AppResources.Import_Body_NoConflicts
                : string.Format(AppResources.Import_Body_Format, conflicts);

        ImportButtonLabel = selected == 1
            ? AppResources.Import_Cta_One
            : string.Format(AppResources.Import_Cta_Format, selected);
    }

    /// <summary>Picker → either straight to the summary, or through the conflicts one at a time.</summary>
    [RelayCommand]
    private async Task ConfirmAsync()
    {
        _Resolutions.Clear();
        _Conflicts = Incoming.Where(i => i.IsSelected && i.IsConflict).ToList();
        _ConflictIndex = 0;
        ApplyToAll = false;

        if (_Conflicts.Count == 0)
        {
            await ApplyAsync();
            return;
        }

        Step = ImportStep.Conflict;
        ShowConflict();
    }

    private void ShowConflict()
    {
        var conflict = _Conflicts[_ConflictIndex];

        ConflictEyebrow = string.Format(
            AppResources.Conflict_Eyebrow_Format, _ConflictIndex + 1, _Conflicts.Count);
        ConflictTitle = string.Format(
            AppResources.Conflict_Title_Format,
            conflict.Incoming.EntryDate.ToString("d MMMM", CultureInfo.CurrentCulture));

        LocalWords = conflict.LocalWords;
        LocalPreview = conflict.LocalPreview;
        IncomingWords = conflict.IncomingWords;
        IncomingPreview = conflict.IncomingPreview;
        Choice = ConflictChoice.Incoming;
    }

    [RelayCommand]
    private void Pick(ConflictChoice choice) => Choice = choice;

    [RelayCommand]
    private void ToggleApplyToAll() => ApplyToAll = !ApplyToAll;

    [RelayCommand]
    private void ToggleSelection(IncomingEntry entry) => entry.IsSelected = !entry.IsSelected;

    /// <summary>Leave this date exactly as it is and move on.</summary>
    [RelayCommand]
    private Task SkipConflictAsync()
    {
        _Resolutions[_Conflicts[_ConflictIndex]] = ConflictChoice.Device;
        return AdvanceAsync();
    }

    [RelayCommand]
    private Task ResolveAsync()
    {
        if (ApplyToAll)
        {
            foreach (var remaining in _Conflicts.Skip(_ConflictIndex))
                _Resolutions[remaining] = Choice;

            return ApplyAsync();
        }

        _Resolutions[_Conflicts[_ConflictIndex]] = Choice;
        return AdvanceAsync();
    }

    private Task AdvanceAsync()
    {
        _ConflictIndex++;

        if (_ConflictIndex >= _Conflicts.Count)
            return ApplyAsync();

        ShowConflict();
        return Task.CompletedTask;
    }

    /// <summary>Writes everything the user agreed to, then shows the summary.</summary>
    private async Task ApplyAsync()
    {
        var imported = 0;   // new pages with no local counterpart
        var written = 0;    // conflicts that actually changed the journal
        var resolved = 0;   // conflicts the user made a decision about

        foreach (var item in Incoming.Where(i => i.IsSelected))
        {
            if (!item.IsConflict)
            {
                item.Incoming.IsUploaded = true;
                await _Database.SaveEntryAsync(item.Incoming);
                imported++;
                continue;
            }

            resolved++;
            switch (_Resolutions.GetValueOrDefault(item, ConflictChoice.Device))
            {
                case ConflictChoice.Incoming:
                    var local = item.Local!;
                    local.Text = item.Incoming.Text;
                    local.NotionPageId = item.Incoming.NotionPageId;
                    local.IsUploaded = true;
                    await _Database.SaveEntryAsync(local);
                    written++;
                    break;

                case ConflictChoice.Both:
                    item.Incoming.IsUploaded = true;
                    await _Database.SaveEntryAsync(item.Incoming);
                    written++;
                    break;

                // Device: the local entry wins, so nothing is written.
            }
        }

        var returned = imported + written;
        DoneTitle = returned == 1
            ? AppResources.ImportDone_Title_One
            : string.Format(AppResources.ImportDone_Title_Format, returned);
        DoneBody = string.Format(AppResources.ImportDone_Body_Format, imported, resolved);

        Step = ImportStep.Done;
    }

    [RelayCommand]
    private static Task OpenHistoryAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    private static Task OpenTodayAsync() => Shell.Current.GoToAsync($"//{nameof(TodayPage)}");

    [RelayCommand]
    private static Task ConnectAsync() => Shell.Current.GoToAsync(nameof(NotionConnectPage));

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    private void BackToPicker()
    {
        Step = ImportStep.Picker;
        RefreshPicker();
    }
}
