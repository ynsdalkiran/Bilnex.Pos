using System;
using System.Linq;
using System.Windows;

namespace Bilnex.Pos.Services;

public static class ThemeManager
{
    private const string DarkThemePath = "Themes/DashboardTheme.xaml";
    private const string LightThemePath = "Themes/LightTheme.xaml";

    public static void ApplyTheme(string? themeName)
    {
        var application = Application.Current;

        if (application is null)
        {
            return;
        }

        var selectedThemePath = string.Equals(themeName, "Light", StringComparison.OrdinalIgnoreCase)
            ? LightThemePath
            : DarkThemePath;

        var mergedDictionaries = application.Resources.MergedDictionaries;
        var existingThemeDictionary = mergedDictionaries
            .FirstOrDefault(x => x.Source is not null &&
                                 (x.Source.OriginalString.EndsWith("DashboardTheme.xaml", StringComparison.OrdinalIgnoreCase) ||
                                  x.Source.OriginalString.EndsWith("LightTheme.xaml", StringComparison.OrdinalIgnoreCase)));

        if (existingThemeDictionary is not null &&
            string.Equals(existingThemeDictionary.Source?.OriginalString, selectedThemePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var newThemeDictionary = new ResourceDictionary
        {
            Source = new Uri(selectedThemePath, UriKind.Relative)
        };

        if (existingThemeDictionary is not null)
        {
            var index = mergedDictionaries.IndexOf(existingThemeDictionary);
            mergedDictionaries[index] = newThemeDictionary;
        }
        else
        {
            mergedDictionaries.Add(newThemeDictionary);
        }
    }
}
