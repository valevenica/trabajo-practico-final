using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StockManufactura.Shared;

namespace StockManufactura.Desktop.Infrastructure;

public static class DesktopAssetLoader
{
    private const string LogoPathEnvVar = "STOCKMANUFACTURA_LOGO_PATH";
    private const string IconPathEnvVar = "STOCKMANUFACTURA_ICON_PATH";

    private static readonly string[] EmbeddedLogoUris =
    {
        "pack://application:,,,/Assets/logo%202.png",
        "pack://application:,,,/Assets/logo.png"
    };

    private static readonly string[] LogoFileExtensions = { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };
    private static readonly string[] LogoNamePatterns = { "logo*", "brand*", "integra*" };
    private static readonly string[] IconNamePatterns = { "app*", "icon*", "logo*" };

    public static ImageSource? TryLoadLogoImage()
    {
        var configuredLogoPath = Environment.GetEnvironmentVariable(LogoPathEnvVar);
        if (!string.IsNullOrWhiteSpace(configuredLogoPath))
        {
            var configuredLogo = TryCreateImageSource(configuredLogoPath.Trim());
            if (configuredLogo is not null)
            {
                return configuredLogo;
            }
        }

        foreach (var candidate in EnumerateLogoCandidates())
        {
            var image = TryCreateImageSource(candidate);
            if (image is not null)
            {
                return image;
            }
        }

        foreach (var uri in EmbeddedLogoUris)
        {
            var image = TryCreateImageSource(uri);
            if (image is not null)
            {
                return image;
            }
        }

        return null;
    }

    public static ImageSource? TryLoadWindowIcon()
    {
        var configuredIconPath = Environment.GetEnvironmentVariable(IconPathEnvVar);
        if (!string.IsNullOrWhiteSpace(configuredIconPath))
        {
            var configuredIcon = TryCreateIconSource(configuredIconPath.Trim());
            if (configuredIcon is not null)
            {
                return configuredIcon;
            }
        }

        var embeddedIcon = TryCreateIconSource("pack://application:,,,/Assets/app.ico");
        if (embeddedIcon is not null)
        {
            return embeddedIcon;
        }

        foreach (var filePath in EnumerateIconCandidates())
        {
            var icon = TryCreateIconSource(filePath);
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

    private static ImageSource? TryCreateIconSource(string candidate)
    {
        try
        {
            var uri = new Uri(candidate, UriKind.Absolute);
            var frame = BitmapFrame.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            frame.Freeze();
            return frame;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> EnumerateLogoCandidates()
    {
        foreach (var assetsFolder in GetAssetsFolders())
        {
            if (!Directory.Exists(assetsFolder))
            {
                continue;
            }

            foreach (var pattern in LogoNamePatterns)
            {
                foreach (var file in SafeEnumerateFiles(assetsFolder, pattern + ".*"))
                {
                    if (LogoFileExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                    {
                        yield return file;
                    }
                }
            }
        }

        if (File.Exists(AppPaths.SplashLogoPath))
        {
            yield return AppPaths.SplashLogoPath;
        }
    }

    private static IEnumerable<string> EnumerateIconCandidates()
    {
        foreach (var assetsFolder in GetAssetsFolders())
        {
            if (!Directory.Exists(assetsFolder))
            {
                continue;
            }

            foreach (var pattern in IconNamePatterns)
            {
                foreach (var file in SafeEnumerateFiles(assetsFolder, pattern + ".ico"))
                {
                    yield return file;
                }
            }
        }
    }

    private static IEnumerable<string> GetAssetsFolders()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Assets");
        yield return AppPaths.AssetsDirectory;
    }

    private static IEnumerable<string> SafeEnumerateFiles(string folderPath, string searchPattern)
    {
        try
        {
            return Directory.EnumerateFiles(folderPath, searchPattern, SearchOption.TopDirectoryOnly);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
