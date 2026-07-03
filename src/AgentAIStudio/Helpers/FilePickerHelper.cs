using Windows.Storage.Pickers;
using Windows.Storage;

namespace AgentAIStudio.Helpers;

public static class FilePickerHelper
{
    public static async Task<string?> PickImageFileAsync()
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.Thumbnail,
            FileTypeFilter = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }
        };

        var window = App.GetWindowHandle();
        if (window != IntPtr.Zero)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, window);
        }

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            return file.Path;
        }
        return null;
    }

    public static async Task<List<string>> PickMultipleImageFilesAsync()
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.Thumbnail,
            FileTypeFilter = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }
        };

        var window = App.GetWindowHandle();
        if (window != IntPtr.Zero)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, window);
        }

        var files = await picker.PickMultipleFilesAsync();
        return files.Select(f => f.Path).ToList();
    }

    public static async Task<string?> SaveFileAsync(string suggestedName, string fileType)
    {
        var picker = new FileSavePicker
        {
            SuggestedFileName = suggestedName,
            DefaultFileExtension = fileType,
            FileTypeChoices = { { fileType switch
            {
                ".jpg" => "JPEG Image",
                ".png" => "PNG Image",
                ".mp4" => "MP4 Video",
                _ => "File"
            }, new List<string> { fileType } } }
        };

        var window = App.GetWindowHandle();
        if (window != IntPtr.Zero)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, window);
        }

        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }
}
