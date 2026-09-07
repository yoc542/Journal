using CommunityToolkit.Mvvm.ComponentModel;
using JournalApp.Localization;
using JournalApp.Models;

namespace JournalApp.ViewModels;

public partial class IntentionDraft : ObservableObject
{
    [ObservableProperty] private bool _IsOpen;
    [ObservableProperty] private string _Note = string.Empty;
    [ObservableProperty] private string _Heading = string.Empty;
    [ObservableProperty] private string _SaveLabel = string.Empty;
    [ObservableProperty] private bool _HasError;

    [ObservableProperty] private string _Title = string.Empty;

    public int EditingId { get; private set; }

    public int MaxTitleLength => Constants.MaxIntentionTitleLength;
    public int MaxNoteLength => Constants.MaxIntentionNoteLength;

    public void OpenNew()
    {
        EditingId = 0;
        Title = string.Empty;
        Note = string.Empty;
        Heading = AppResources.Intent_Draft_New_Heading;
        SaveLabel = AppResources.Intent_Draft_Save_New;
        HasError = false;
        IsOpen = true;
    }

    public void OpenEdit(Intention intention)
    {
        EditingId = intention.Id;
        Title = intention.Title;
        Note = intention.Note;
        Heading = AppResources.Intent_Draft_Edit_Heading;
        SaveLabel = AppResources.Intent_Draft_Save_Edit;
        HasError = false;
        IsOpen = true;
    }

    public void Close()
    {
        IsOpen = false;
        HasError = false;
    }

    public bool Validate()
    {
        HasError = Title.Trim().Length == 0;
        return !HasError;
    }

    public void ApplyTo(Intention intention)
    {
        intention.Title = Title.Trim();
        intention.Note = Note.Trim();
    }

    partial void OnTitleChanged(string value)
    {
        if (HasError && value.Trim().Length > 0)
            HasError = false;
    }
}
