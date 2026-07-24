using System.Globalization;
using System.Reflection;
using System.Resources;

namespace JournalApp.Resources.Strings;

/// <summary>Strongly-typed accessors over AppResources.resx, read via a plain ResourceManager
/// (hand-written instead of VS-generated, since this project builds outside Visual Studio).</summary>
public static class AppResources
{
    private static readonly ResourceManager Manager =
        new("JournalApp.Resources.Strings.AppResources", Assembly.GetExecutingAssembly());

    private static string Get(string name) => Manager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    public static string AppTitle => Get(nameof(AppTitle));
    public static string Today_Title => Get(nameof(Today_Title));
    public static string Entry_Day_Format => Get(nameof(Entry_Day_Format));
    public static string Editor_Placeholder => Get(nameof(Editor_Placeholder));
    public static string CharacterCount_Format => Get(nameof(CharacterCount_Format));
    public static string History_ToolbarItem => Get(nameof(History_ToolbarItem));
    public static string History_Title => Get(nameof(History_Title));
    public static string History_Empty_Title => Get(nameof(History_Empty_Title));
    public static string History_Empty_Subtitle => Get(nameof(History_Empty_Subtitle));
    public static string History_Uploaded_Badge => Get(nameof(History_Uploaded_Badge));
    public static string Menu_Cancel => Get(nameof(Menu_Cancel));
    public static string Menu_UploadToNotion => Get(nameof(Menu_UploadToNotion));
    public static string Menu_Delete => Get(nameof(Menu_Delete));
    public static string Delete_Title => Get(nameof(Delete_Title));
    public static string Delete_Message => Get(nameof(Delete_Message));
    public static string Delete_Confirm => Get(nameof(Delete_Confirm));
    public static string Upload_AlreadyTitle => Get(nameof(Upload_AlreadyTitle));
    public static string Upload_AlreadyMessage => Get(nameof(Upload_AlreadyMessage));
    public static string Upload_Confirm => Get(nameof(Upload_Confirm));
    public static string Upload_SuccessTitle => Get(nameof(Upload_SuccessTitle));
    public static string Upload_SuccessMessage => Get(nameof(Upload_SuccessMessage));
    public static string Upload_FailTitle => Get(nameof(Upload_FailTitle));
    public static string Import_ToolbarItem => Get(nameof(Import_ToolbarItem));
    public static string Import_SuccessTitle => Get(nameof(Import_SuccessTitle));
    public static string Import_SuccessMessage_Format => Get(nameof(Import_SuccessMessage_Format));
    public static string Import_FailTitle => Get(nameof(Import_FailTitle));
    public static string OK => Get(nameof(OK));
    public static string Untitled_Entry => Get(nameof(Untitled_Entry));
}
