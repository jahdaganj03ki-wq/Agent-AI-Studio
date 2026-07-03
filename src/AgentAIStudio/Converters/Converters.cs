using Microsoft.UI.Xaml.Data;
using AgentAIStudio.Models;

namespace AgentAIStudio.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is VideoTaskStatus status)
        {
            return status switch
            {
                VideoTaskStatus.Queued => "#FFA500",
                VideoTaskStatus.InProgress => "#0078D4",
                VideoTaskStatus.Completed => "#107C10",
                VideoTaskStatus.Failed => "#D13438",
                _ => "#666666"
            };
        }
        return "#666666";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class StatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is VideoTaskStatus status)
        {
            return status switch
            {
                VideoTaskStatus.Queued => "Queued",
                VideoTaskStatus.InProgress => "Generating...",
                VideoTaskStatus.Completed => "Completed",
                VideoTaskStatus.Failed => "Failed",
                _ => "Unknown"
            };
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return b ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        return Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b) return !b;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
