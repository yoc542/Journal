namespace JournalApp.Services;

public class NavigationService
{
    public Task PushAsync(string route, IDictionary<string, object>? parameters = null) =>
        parameters is null
            ? Shell.Current.GoToAsync(route)
            : Shell.Current.GoToAsync(route, parameters);

    public Task BackAsync() => Shell.Current.GoToAsync("..");

    public async Task ResetToAsync(string route)
    {
        await Shell.Current.Navigation.PopToRootAsync();

        if (!string.Equals(CurrentRoot, route, StringComparison.Ordinal))
            await Shell.Current.GoToAsync($"//{route}");
    }

    private static string CurrentRoot =>
        Shell.Current.CurrentItem?.CurrentItem?.CurrentItem?.Route ?? string.Empty;
}
