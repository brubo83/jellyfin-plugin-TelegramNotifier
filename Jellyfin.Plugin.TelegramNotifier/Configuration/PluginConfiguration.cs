using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TelegramNotifier.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public PluginConfiguration()
    {
        EnablePlugin = true;
        BotToken = string.Empty;
        ChatId = string.Empty;
        ThreadId = string.Empty;
        ServerUrl = string.Empty;
        ServerDisplayUrl = string.Empty;
        EmptyPlaceholderReplacement = "...";
        PlaybackProgressIntervalMinutes = 15;
        Rules = Array.Empty<NotificationRule>();
        UserConfigurations = Array.Empty<UserConfiguration>();
    }

    public bool EnablePlugin { get; set; }

    /// <summary>
    /// Gets or sets the token of the single Telegram bot used for every notification.
    /// </summary>
    public string BotToken { get; set; }

    /// <summary>
    /// Gets or sets the single chat / channel / group that receives every notification.
    /// </summary>
    public string ChatId { get; set; }

    /// <summary>
    /// Gets or sets the optional topic (message_thread_id) inside a forum group.
    /// </summary>
    public string ThreadId { get; set; }

    public bool SilentNotification { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether episodes use the series poster instead of the episode thumbnail.
    /// </summary>
    public bool KeepSerieImage { get; set; }

    /// <summary>
    /// Gets or sets an optional internal URL of Jellyfin (e.g. http://127.0.0.1:8096), used only as a
    /// last-resort fallback to fetch images. Images are normally read straight from disk or through
    /// Jellyfin's own local address, so this can stay empty even behind a reverse proxy.
    /// </summary>
    public string ServerUrl { get; set; }

    /// <summary>
    /// Gets or sets the public URL used in the {server.Url} placeholder (e.g. https://jellyfin.example.com).
    /// </summary>
    public string ServerDisplayUrl { get; set; }

    /// <summary>
    /// Gets or sets the replacement for empty placeholders: "" (blank), "...", "N/A", or "—".
    /// </summary>
    public string EmptyPlaceholderReplacement { get; set; }

    /// <summary>
    /// Gets or sets how often (minutes) a "still watching" notification may be sent for the same playback.
    /// Jellyfin reports progress every ~10 seconds; the first message comes after this interval, not at start.
    /// </summary>
    public int PlaybackProgressIntervalMinutes { get; set; }

    public bool ProxyEnabled { get; set; }

    public string? ProxyHost { get; set; }

    public int ProxyPort { get; set; }

    public string? ProxyUsername { get; set; }

    public string? ProxyPassword { get; set; }

    /// <summary>
    /// Gets or sets the notification matrix (one entry per notification type).
    /// </summary>
    public NotificationRule[] Rules { get; set; }

    /// <summary>
    /// Gets or sets the per-user configuration of the original plugin. Only read once to migrate the
    /// bot token and chat id; not used otherwise.
    /// </summary>
    public UserConfiguration[] UserConfigurations { get; set; }
}
