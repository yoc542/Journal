using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JournalApp.Data;
using JournalApp.Localization;
using JournalApp.Models;
using JournalApp.Services;
using JournalApp.Views;

namespace JournalApp.ViewModels;

public enum UploadState { Idle, Running, Done, Failed }

/// <summary>One row of the upload queue, updated live as its entry is sent.</summary>
public partial class UploadQueueItem : ObservableObject
{
    public UploadQueueItem(JournalEntry entry)
    {
        Entry = entry;
        DateLabel = entry.FullDateLabel;
        StateLabel = AppResources.Upload_Item_Queued;
    }

    public JournalEntry Entry { get; }

    public string DateLabel { get; }

    [ObservableProperty] private string _StateLabel;
    [ObservableProperty] private bool _IsSending;
    [ObservableProperty] private bool _IsUploaded;
    [ObservableProperty] private bool _IsBlocked;

    public void MarkQueued() => Set(AppResources.Upload_Item_Queued, false, false, false);
    public void MarkSending() => Set(AppResources.Upload_Item_Sending, true, false, false);
    public void MarkUploaded() => Set(AppResources.Upload_Item_Uploaded, false, true, false);
    public void MarkBlocked() => Set(AppResources.Upload_Item_Retry, false, false, true);

    private void Set(string label, bool sending, bool uploaded, bool blocked)
    {
        StateLabel = label;
        IsSending = sending;
        IsUploaded = uploaded;
        IsBlocked = blocked;
    }
}

public partial class UploadViewModel : ObservableObject
{
    private readonly JournalDatabase _Database;
    private readonly NotionService _Notion;

    [ObservableProperty] private ObservableCollection<UploadQueueItem> _Queue = new();
    [ObservableProperty] private string _Heading = string.Empty;
    [ObservableProperty] private string _Subheading = string.Empty;
    [ObservableProperty] private string _StatusLabel = string.Empty;
    [ObservableProperty] private string _PercentLabel = string.Empty;
    [ObservableProperty] private string _ButtonLabel = string.Empty;
    [ObservableProperty] private string _DoneBanner = string.Empty;
    [ObservableProperty] private double _Progress;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotConnected))]
    private bool _IsConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDone), nameof(IsFailed), nameof(IsNotRunning), nameof(HasQueue))]
    private UploadState _State = UploadState.Idle;

    public UploadViewModel(JournalDatabase database, NotionService notion)
    {
        _Database = database;
        _Notion = notion;
    }

    public bool IsNotConnected => !IsConnected;

    public bool IsDone => State == UploadState.Done;
    public bool IsFailed => State == UploadState.Failed;
    public bool IsNotRunning => State != UploadState.Running;

    /// <summary>Hides the progress card when there was never anything to send.</summary>
    public bool HasQueue => Queue.Count > 0;

    public async Task LoadAsync()
    {
        IsConnected = await NotionService.IsConnectedAsync();
        if (!IsConnected)
            return;

        var pending = await _Database.GetPendingEntriesAsync();
        Queue = new ObservableCollection<UploadQueueItem>(pending.Select(e => new UploadQueueItem(e)));
        State = UploadState.Idle;
        Progress = 0;
        Refresh();
    }

    /// <summary>Recomputes every label from the current state and queue.</summary>
    private void Refresh()
    {
        var total = Queue.Count;
        var sent = Queue.Count(q => q.IsUploaded);

        Heading = State switch
        {
            UploadState.Running => AppResources.Upload_Heading_Running,
            UploadState.Done => AppResources.Upload_Heading_Done,
            UploadState.Failed => AppResources.Upload_Heading_Failed,
            _ => total switch
            {
                0 => AppResources.Upload_Heading_Empty,
                1 => AppResources.Upload_Heading_Idle_One,
                _ => string.Format(AppResources.Upload_Heading_Idle_Format, total),
            },
        };

        Subheading = State switch
        {
            UploadState.Done => AppResources.Upload_Sub_Done,
            UploadState.Failed => AppResources.Upload_Sub_Failed,
            _ => total == 0 ? AppResources.Upload_Sub_Empty : AppResources.Upload_Sub_Idle,
        };

        StatusLabel = State switch
        {
            UploadState.Running => AppResources.Upload_Status_Uploading,
            UploadState.Done => AppResources.Upload_Status_Complete,
            UploadState.Failed => AppResources.Upload_Status_Stopped,
            _ => AppResources.Upload_Status_Ready,
        };

        var remaining = total - sent;
        ButtonLabel = State switch
        {
            UploadState.Running => AppResources.Upload_Cta_Running,
            UploadState.Done => AppResources.Upload_Cta_Done,
            UploadState.Failed => AppResources.Upload_Cta_Retry,
            _ => remaining switch
            {
                0 => AppResources.Upload_Cta_Done,
                1 => AppResources.Upload_Cta_Start_One,
                _ => string.Format(AppResources.Upload_Cta_Start_Format, remaining),
            },
        };

        DoneBanner = sent == 1
            ? AppResources.Upload_Done_Banner_One
            : string.Format(AppResources.Upload_Done_Banner_Format, sent);

        Progress = total == 0 ? 0 : (double)sent / total;
        PercentLabel = $"{(int)Math.Round(Progress * 100)}%";
        OnPropertyChanged(nameof(HasQueue));
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        // In the terminal states the button leaves the screen or retries what is left.
        if (State is UploadState.Running)
            return;

        if (State is UploadState.Done || Queue.All(q => q.IsUploaded))
        {
            await BackAsync();
            return;
        }

        foreach (var blocked in Queue.Where(q => q.IsBlocked))
            blocked.MarkQueued();

        State = UploadState.Running;
        Refresh();

        foreach (var item in Queue.Where(q => !q.IsUploaded).ToList())
        {
            item.MarkSending();
            try
            {
                item.Entry.NotionPageId = await _Notion.UploadEntryAsync(item.Entry);
                item.Entry.IsUploaded = true;
                await _Database.SaveEntryAsync(item.Entry);
                item.MarkUploaded();
                Refresh();
            }
            catch
            {
                // Stop at the first failure: the rest stay queued for the retry.
                item.MarkBlocked();
                foreach (var rest in Queue.Where(q => !q.IsUploaded && q != item))
                    rest.MarkBlocked();

                State = UploadState.Failed;
                Refresh();
                return;
            }
        }

        State = UploadState.Done;
        Refresh();
    }

    [RelayCommand]
    private static Task ConnectAsync() => Shell.Current.GoToAsync(nameof(NotionConnectPage));

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");
}
