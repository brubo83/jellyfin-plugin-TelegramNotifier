using System;

namespace Jellyfin.Plugin.TelegramNotifier.Configuration;

/// <summary>
/// One row of the matrix as stored in the plugin XML configuration.
/// </summary>
public class NotificationRule
{
    public NotificationRule()
    {
        Key = string.Empty;
        UserIds = Array.Empty<string>();
    }

    /// <summary>
    /// Gets or sets the notification key (see <see cref="NotificationCatalog"/>).
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a server-wide notification is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin user ids (32 hex chars, no dashes) for which a user notification is enabled.
    /// </summary>
    public string[] UserIds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the message is sent without sound.
    /// Null means the default of the notification type.
    /// </summary>
    public bool? Silent { get; set; }

    /// <summary>
    /// Gets or sets a custom message template. Empty means the default template.
    /// </summary>
    public string? Message { get; set; }
}
