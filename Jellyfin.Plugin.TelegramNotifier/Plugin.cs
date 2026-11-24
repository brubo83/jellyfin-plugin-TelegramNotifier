using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.TelegramNotifier.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TelegramNotifier;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<Plugin> _logger;

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
    {
        _logger = logger;
        Instance = this;
        MigrateLegacyConfiguration();
    }

    public static ILogger<Plugin> Logger => Instance!._logger;

    public static PluginConfiguration Config => Instance!.Configuration;

    public override string Name => "Telegram Notifier";

    public override Guid Id => Guid.Parse("fd76211d-17e0-4a72-a23f-c6eeb1e48b3a");

    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// The original plugin stored one bot/chat per Jellyfin user. This fork uses a single bot and chat:
    /// take them from the first legacy entry that has them, and create the matrix rows.
    /// </summary>
    private void MigrateLegacyConfiguration()
    {
        var config = Configuration;
        bool changed = false;

        if (string.IsNullOrEmpty(config.BotToken) && config.UserConfigurations is { Length: > 0 })
        {
            var legacy = config.UserConfigurations
                .Where(u => !string.IsNullOrEmpty(u.BotToken) && !string.IsNullOrEmpty(u.ChatId))
                .OrderByDescending(u => u.EnableUser)
                .FirstOrDefault();
            if (legacy is not null)
            {
                config.BotToken = legacy.BotToken;
                config.ChatId = legacy.ChatId;
                config.ThreadId = legacy.ThreadId;
                config.SilentNotification = legacy.SilentNotification;
                changed = true;
                _logger.LogInformation("TelegramNotifier: migrated bot token and chat id from the per-user configuration.");
            }
        }

        if (config.UserConfigurations is { Length: > 0 })
        {
            config.UserConfigurations = Array.Empty<UserConfiguration>();
            changed = true;
        }

        var rules = (config.Rules ?? Array.Empty<NotificationRule>()).ToList();
        foreach (var definition in NotificationCatalog.All)
        {
            if (!rules.Any(r => r.Key == definition.Key))
            {
                rules.Add(new NotificationRule { Key = definition.Key });
                changed = true;
            }
        }

        if (changed)
        {
            config.Rules = rules.ToArray();
            SaveConfiguration();
        }
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = this.Name,
            EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.Web.configPage.html", GetType().Namespace)
        };
        yield return new PluginPageInfo
        {
            Name = $"{this.Name}.js",
            EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.Web.configPage.js", GetType().Namespace)
        };
    }
}