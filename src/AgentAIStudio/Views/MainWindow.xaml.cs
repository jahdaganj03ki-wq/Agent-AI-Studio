using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using AgentAIStudio.Controls;
using AgentAIStudio.Views;

namespace AgentAIStudio.Views;

public sealed partial class MainWindow : Window
{
    private Frame? _contentFrame;
    private readonly Dictionary<string, Type> _pages = new()
    {
        ["chat"] = typeof(ChatPage),
        ["images"] = typeof(ImagesPage),
        ["video"] = typeof(VideoPage),
        ["imagestudio"] = typeof(ImageStudioPage),
        ["settings"] = typeof(SettingsPage),
    };

    public MainWindow()
    {
        InitializeComponent();
        Title = "Agent AI Studio";
        ExtendsContentIntoTitleBar = true;

        var root = (Grid)Content;
    }

    private void SetTitleBar(Grid root)
    {
        var titleBar = new Grid
        {
            Height = 32,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(64, 0, 0, 0),
        };
        ColumnDefinition.SetColumn(titleBar, 1);
        root.Children.Add(titleBar);
    }

    private void SidebarNav_OnCategorySelected(object sender, string category)
    {
        if (_contentFrame == null) return;

        if (category == "settings")
        {
            _contentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (_pages.TryGetValue(category, out var pageType))
        {
            _contentFrame.Navigate(pageType);
        }
    }
}
