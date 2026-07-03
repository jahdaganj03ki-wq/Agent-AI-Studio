using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace AgentAIStudio.Helpers;

public static class ImageHelper
{
    public static async Task<BitmapImage?> Base64ToBitmapAsync(string? base64)
    {
        if (string.IsNullOrEmpty(base64)) return null;

        try
        {
            var bytes = Convert.FromBase64String(base64);
            using var ms = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            using var randomStream = new MemoryStream(bytes).AsRandomAccessStream();
            await bitmap.SetSourceAsync(randomStream);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public static async Task<string> FileToBase64Async(string filePath)
    {
        var bytes = await File.ReadAllBytesAsync(filePath);
        return Convert.ToBase64String(bytes);
    }

    public static async Task<BitmapImage?> UrlToBitmapAsync(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        try
        {
            var bitmap = new BitmapImage(new Uri(url));
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public static async Task<byte[]> DownloadImageAsync(string url)
    {
        using var client = new HttpClient();
        return await client.GetByteArrayAsync(url);
    }

    public static string GetImageExtension(string? url)
    {
        if (string.IsNullOrEmpty(url)) return ".png";
        var ext = Path.GetExtension(url)?.ToLower();
        return ext switch
        {
            ".jpg" or ".jpeg" => ".jpg",
            ".gif" => ".gif",
            ".webp" => ".webp",
            _ => ".png"
        };
    }
}
