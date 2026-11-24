using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.TelegramNotifier.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TelegramNotifier
{
    public class NotificationFilter
    {
        private readonly Sender _sender;
        private readonly ImageProvider _imageProvider;
        private readonly ILogger<Plugin> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IServerApplicationHost _applicationHost;

        public NotificationFilter(
            Sender sender,
            ImageProvider imageProvider,
            ILibraryManager libraryManager,
            IMediaSourceManager mediaSourceManager,
            IServerApplicationHost applicationHost)
        {
            _applicationHost = applicationHost;
            _sender = sender;
            _imageProvider = imageProvider;
            _libraryManager = libraryManager;
            _mediaSourceManager = mediaSourceManager;
            _logger = Plugin.Logger;
        }

        public enum NotificationType
        {
            ItemAdded,
            ItemDeleted,
            PlaybackStart,
            PlaybackProgress,
            PlaybackStop,
            SubtitleDownloadFailure,
            AuthenticationFailure,
            AuthenticationSuccess,
            SessionStart,
            PendingRestart,
            TaskCompleted,
            PluginInstallationCancelled,
            PluginInstallationFailed,
            PluginInstalled,
            PluginInstalling,
            PluginUninstalled,
            PluginUpdated,
            UserCreated,
            UserDeleted,
            UserLockedOut,
            UserPasswordChanged,
            UserUpdated
        }

        public static string NormalizeUserId(string? userId) =>
            (userId ?? string.Empty).Replace("-", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();

        public static string NormalizeUserId(Guid userId) => userId.ToString("N");

        /// <summary>
        /// Checks the matrix and sends at most one message to the configured chat.
        /// </summary>
        /// <param name="type">Notification family.</param>
        /// <param name="eventArgs">Event data used by the message placeholders.</param>
        /// <param name="userIds">Jellyfin users that caused the event (for user notifications).</param>
        /// <param name="imageItem">Item whose poster is attached, or null for a text-only message.</param>
        /// <param name="subtype">Specific key (e.g. PlaybackStartMovies) when the family has subtypes.</param>
        /// <param name="linkItem">Item for the "Apri in Jellyfin" button (defaults to <paramref name="imageItem"/>).</param>
        public async Task Filter(
            NotificationType type,
            object eventArgs,
            IEnumerable<string>? userIds = null,
            BaseItem? imageItem = null,
            string subtype = "",
            BaseItem? linkItem = null)
        {
            var config = Plugin.Config;
            if (!config.EnablePlugin)
            {
                return;
            }

            string key = string.IsNullOrEmpty(subtype) ? type.ToString() : subtype;
            NotificationDefinition? definition = NotificationCatalog.Find(key);
            if (definition is null)
            {
                _logger.LogDebug("Unknown notification key {Key}", key);
                return;
            }

            NotificationRule? rule = config.Rules?.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.Ordinal));
            if (rule is null || !IsEnabled(definition, rule, userIds))
            {
                return;
            }

            string template = string.IsNullOrWhiteSpace(rule.Message) ? definition.DefaultMessage : rule.Message;
            string message = MessageParser.ParseMessage(template, eventArgs, _libraryManager, _mediaSourceManager);
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            // A single chat receives everything: never post the same message twice in a row.
            if (!NotificationDeduplicator.ShouldSend($"{key}|{message}"))
            {
                _logger.LogInformation("Duplicate notification skipped ({Key})", key);
                return;
            }

            bool silent = config.SilentNotification || (rule.Silent ?? definition.DefaultSilent);
            var target = new TelegramTarget(config.BotToken, config.ChatId, config.ThreadId, silent);
            string? buttonUrl = GetItemUrl(linkItem ?? imageItem);
            try
            {
                if (imageItem is null)
                {
                    await _sender.SendMessage(key, message, target, buttonUrl).ConfigureAwait(false);
                    return;
                }

                ImagePayload? image = await _imageProvider
                    .GetPrimaryImageAsync(ImageProvider.ResolveImageItem(imageItem))
                    .ConfigureAwait(false);
                await _sender.SendMessageWithPhoto(key, message, image, target, buttonUrl).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // A failing notification must never break Jellyfin event handling.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "An error occurred while sending notification {Key}", key);
            }
        }

        /// <summary>
        /// Link to the item in the Jellyfin web client, built from the public URL in the settings
        /// ("URL pubblico"). No public URL, no button.
        /// </summary>
        private string? GetItemUrl(BaseItem? item) => BuildItemUrl(item, _applicationHost.SystemId);

        public static string? BuildItemUrl(BaseItem? item, string? serverId)
        {
            string publicUrl = Plugin.Config.ServerDisplayUrl?.Trim() ?? string.Empty;
            if (item is null || string.IsNullOrEmpty(publicUrl))
            {
                return null;
            }

            if (!publicUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !publicUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                publicUrl = "https://" + publicUrl;
            }

            if (!Uri.TryCreate(publicUrl.TrimEnd('/'), UriKind.Absolute, out Uri? baseUri))
            {
                return null;
            }

            string url = $"{baseUri.ToString().TrimEnd('/')}/web/#/details?id={item.Id:N}";
            if (!string.IsNullOrEmpty(serverId))
            {
                url += "&serverId=" + Uri.EscapeDataString(serverId);
            }

            return url;
        }

        private static bool IsEnabled(NotificationDefinition definition, NotificationRule rule, IEnumerable<string>? userIds)
        {
            if (definition.Scope == NotificationScope.Server)
            {
                return rule.Enabled;
            }

            if (userIds is null || rule.UserIds is null || rule.UserIds.Length == 0)
            {
                return false;
            }

            var enabled = new HashSet<string>(rule.UserIds.Select(NormalizeUserId));
            return userIds.Select(NormalizeUserId).Any(enabled.Contains);
        }
    }
}
