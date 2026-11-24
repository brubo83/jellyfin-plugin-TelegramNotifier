namespace Jellyfin.Plugin.TelegramNotifier.Configuration;

/// <summary>
/// Legacy per-user configuration of the original plugin, kept only so old XML files still load and
/// their bot token / chat id can be migrated to the new single-bot configuration.
/// </summary>
public class UserConfiguration
{
    public UserConfiguration()
    {
        UserId = string.Empty;
        UserName = string.Empty;
        BotToken = string.Empty;
        ChatId = string.Empty;
        ThreadId = string.Empty;
    }

    public string UserId { get; set; }

    public string UserName { get; set; }

    public string BotToken { get; set; }

    public string ChatId { get; set; }

    public string ThreadId { get; set; }

    public bool EnableUser { get; set; }

    public bool SilentNotification { get; set; }
}
