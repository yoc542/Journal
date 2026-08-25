using System.Globalization;
using JournalApp.Localization;

namespace JournalApp.Extensions;

/// <summary>
/// XAML markup extension for localized text: <c>Text="{localize:Translate Today_Page_Eyebrow}"</c>.
/// Resolves the key against the resx files in <see cref="Localization"/> through an indexer,
/// so XAML never has to name the generated accessor property.
/// </summary>
[ContentProperty(nameof(Name))]
public class TranslateExtension : IMarkupExtension<BindingBase>
{
    /// <summary>Resource key to look up.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Indexer the returned binding reads. Falls back to the key itself so a
    /// missing or misspelled resource is obvious on screen instead of rendering blank.</summary>
    public object this[string resourceKey] =>
        AppResources.ResourceManager.GetString(resourceKey, AppResources.Culture ?? CultureInfo.CurrentUICulture)
        ?? resourceKey;

    public BindingBase ProvideValue(IServiceProvider serviceProvider) =>
        new Binding
        {
            Mode = BindingMode.OneWay,
            Path = $"[{Name}]",
            Source = this,
        };

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) =>
        ProvideValue(serviceProvider);
}
