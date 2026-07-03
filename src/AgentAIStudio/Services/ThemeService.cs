using Microsoft.UI.Xaml;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.Services;

public class ThemeService
{
    private const string ThemeSettingKey = "ApplicationTheme";

    public ElementTheme CurrentTheme { get; private set; } = ElementTheme.Default;

    public void Initialize()
    {
        if (Application.Current is not App app) return;
        var themeStr = LoadThemeSetting();
        CurrentTheme = themeStr switch
        {
            "Dark" => ElementTheme.Dark,
            "Light" => ElementTheme.Light,
            _ => ElementTheme.Default
        };
        ApplyTheme(CurrentTheme);
    }

    public void SetTheme(ElementTheme theme)
    {
        CurrentTheme = theme;
        SaveThemeSetting(theme.ToString());
        ApplyTheme(theme);
    }

    public void ToggleTheme()
    {
        SetTheme(CurrentTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark);
    }

    private static void ApplyTheme(ElementTheme theme)
    {
        if (Application.Current is App app && app.MainWindow?.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }
    }

    private static string LoadThemeSetting()
    {
        var path = GetSettingsPath();
        return File.Exists(path) ? File.ReadAllText(path).Trim() : "Dark";
    }

    private static void SaveThemeSetting(string theme)
    {
        var path = GetSettingsPath();
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, theme);
    }

    private static string GetSettingsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AgentAIStudio", "theme.txt");
    }
}
