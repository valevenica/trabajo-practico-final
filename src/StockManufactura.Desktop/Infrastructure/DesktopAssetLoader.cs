using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StockManufactura.Shared;

namespace StockManufactura.Desktop.Infrastructure;

public static class DesktopAssetLoader
{
    private static readonly string[] EmbeddedLogoUris =
    {
        "pack://application:,,,/Assets/logo%202.png",
        "pack://application:,,,/Assets/logo.png"
    };

    public static ImageSource? TryLoadLogoImage()
    {
        foreach (var uri in EmbeddedLogoUris)
        {
            var image = TryCreateImageSource(uri);
            if (image is not null)
            {
                return image;
            }
        }

        var fileCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "logo 2.png"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png"),
            Path.Combine(AppPaths.AssetsDirectory, "logo 2.png"),
            AppPaths.SplashLogoPath
        };

        foreach (var filePath in fileCandidates)
        {
            if (!File.Exists(filePath))
            {
                continue;
            }

            var image = TryCreateImageSource(filePath);
            if (image is not null)
            {
                return image;
            }
        }

        return null;
    }

    public static ImageSource? TryLoadWindowIcon()
    {
        var fileCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"),
            Path.Combine(AppPaths.AssetsDirectory, "app.ico")
        };

        foreach (var filePath in fileCandidates)
        {
            if (!File.Exists(filePath))
            {
                continue;
            }

            var icon = TryCreateImageSource(filePath);
            if (icon is not null)
            {
                return icon;
            }
        }

        return null;
    }

    private static ImageSource? TryCreateImageSource(string candidate)
    {
        try
        {
            var uri = new Uri(candidate, UriKind.Absolute);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = uri;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
