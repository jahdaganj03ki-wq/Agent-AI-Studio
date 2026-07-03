using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AgentAIStudio.Controls;

public sealed partial class SidebarNav : UserControl
{
    public event Action<string>? OnCategorySelected;

    private Button? _lastSelected;

    public SidebarNav()
    {
        InitializeComponent();
        SelectCategory("chat");
    }

    private void OnNavClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            SelectCategory(tag);
            OnCategorySelected?.Invoke(tag);
        }
    }

    private void SelectCategory(string category)
    {
        if (_lastSelected != null)
        {
            _lastSelected.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        var button = category switch
        {
            "chat" => ChatBtn,
            "images" => ImagesBtn,
            "video" => VideoBtn,
            "imagestudio" => ImageStudioBtn,
            "settings" => SettingsBtn,
            _ => null
        };

        if (button != null)
        {
            button.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkGray) { Opacity = 0.3 };
            _lastSelected = button;
        }
    }
}
