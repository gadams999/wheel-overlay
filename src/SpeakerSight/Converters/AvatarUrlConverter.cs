using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using OpenDash.OverlayCore.Services;

namespace OpenDash.SpeakerSight.Converters;

/// <summary>
/// Converts [GuildId, UserId, GuildAvatarHash, AvatarHash] to a Discord CDN BitmapImage.
/// Guild-specific avatar takes priority; falls back to global avatar; returns null when no hash is available.
/// </summary>
public class AvatarUrlConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 4) return null;

        var guildId         = values[0] as string;
        var userId          = values[1] as string;
        var guildAvatarHash = values[2] as string;
        var avatarHash      = values[3] as string;

        if (string.IsNullOrEmpty(userId)) return null;

        string? url = null;

        if (!string.IsNullOrEmpty(guildId) && !string.IsNullOrEmpty(guildAvatarHash))
            url = $"https://cdn.discordapp.com/guilds/{guildId}/users/{userId}/avatars/{guildAvatarHash}.png?size=32";
        else if (!string.IsNullOrEmpty(avatarHash))
            url = $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.png?size=32";

        if (url == null) return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource       = new Uri(url);
            image.CacheOption     = BitmapCacheOption.OnLoad;
            image.CreateOptions   = BitmapCreateOptions.None;
            image.EndInit();

            image.DownloadFailed += (_, _) => LogService.Info($"AvatarUrlConverter: Download failed for {url}");
            image.DecodeFailed   += (_, _) => LogService.Info($"AvatarUrlConverter: Decode failed for {url}");

            return image;
        }
        catch (Exception ex)
        {
            LogService.Info($"AvatarUrlConverter: Failed to create BitmapImage for {url}: {ex.Message}");
            return null;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
