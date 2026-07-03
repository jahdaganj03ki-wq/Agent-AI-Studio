using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AgentAIStudio.Models;

namespace AgentAIStudio.Controls;

public sealed partial class VideoQueueItemControl : UserControl
{
    public VideoTaskItem? TaskItem
    {
        get => DataContext as VideoTaskItem;
        set => DataContext = value;
    }

    public VideoQueueItemControl()
    {
        InitializeComponent();
    }

    private void OnPlayClick(object sender, RoutedEventArgs e)
    {
        // Playback handled by parent control
    }

    private void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        // Download handled by parent control
    }
}
