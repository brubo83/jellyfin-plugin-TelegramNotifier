using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TelegramNotifier
{
    /// <summary>
    /// Where to send: the single bot and chat of the configuration (or test values from the settings page).
    /// </summary>
    public sealed class TelegramTarget
    {
        public TelegramTarget(string botToken, string chatId, string? threadId, bool silent)
        {
            BotToken = botToken?.Trim() ?? string.Empty;
            ChatId = chatId?.Trim() ?? string.Empty;
            ThreadId = threadId?.Trim();
            Silent = silent;
        }

        public string BotToken { get; }

        public string ChatId { get; }

        public string? ThreadId { get; }

        public bool Silent { get; }

        public bool IsValid => !string.IsNullOrEmpty(BotToken) && !string.IsNullOrEmpty(ChatId);

        public static TelegramTarget FromConfig()
        {
            var c = Plugin.Config;
            return new TelegramTarget(c.BotToken, c.ChatId, c.ThreadId, c.SilentNotification);
        }
    }

    /// <summary>
    /// A link button shown under a Telegram message.
    /// </summary>
    public sealed class InlineButton
    {
        public InlineButton(string text, string url)
        {
            Text = text;
            Url = url;
        }

        public string Text { get; }

        public string Url { get; }
    }

    public partial class Sender : IDisposable
    {
        // Telegram limit for photo captions.
        private const int MaxCaptionLength = 1024;

        private readonly HttpClient _httpClient;
        private readonly ILogger<Plugin> _logger;

        public Sender()
        {
            _logger = Plugin.Logger;
            var config = Plugin.Config;
#pragma warning disable CA2000, CA5399 // Handler is disposed by HttpClient.
            var handler = new HttpClientHandler();
#pragma warning restore CA2000, CA5399

            if (config.ProxyEnabled && !string.IsNullOrEmpty(config.ProxyHost))
            {
                var proxy = new WebProxy(config.ProxyHost, config.ProxyPort);
                if (!string.IsNullOrEmpty(config.ProxyUsername))
                {
                    proxy.Credentials = new NetworkCredential(config.ProxyUsername, config.ProxyPassword);
                }

                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            _httpClient = new HttpClient(handler, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(30) };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
        }

        /// <summary>
        /// Sends a text message. <paramref name="buttons"/> adds link buttons (Apri in Jellyfin, IMDb); if Telegram
        /// refuses it (e.g. a URL it does not accept), the message is sent again without the button.
        /// </summary>
        public async Task<bool> SendMessage(string notificationType, string message, TelegramTarget target, IReadOnlyList<InlineButton>? buttons = null)
        {
            if (!target.IsValid)
            {
                _logger.LogWarning("({NotificationType}): bot token or chat id not configured, nothing sent.", notificationType);
                return false;
            }

            if (await SendTextCore(notificationType, message, target, buttons).ConfigureAwait(false))
            {
                return true;
            }

            if (buttons is null || buttons.Count == 0)
            {
                return false;
            }

            _logger.LogWarning("({NotificationType}): retrying without buttons ({Urls}).", notificationType, string.Join(", ", buttons.Select(b => b.Url)));
            return await SendTextCore(notificationType, message, target, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Uploads the image bytes with the message as caption. If the photo cannot be sent for any
        /// reason, the message is sent as plain text so the notification is never lost.
        /// </summary>
        public async Task<bool> SendMessageWithPhoto(string notificationType, string message, ImagePayload? image, TelegramTarget target, IReadOnlyList<InlineButton>? buttons = null)
        {
            if (image is null)
            {
                return await SendMessage(notificationType, message, target, buttons).ConfigureAwait(false);
            }

            if (!target.IsValid)
            {
                _logger.LogWarning("({NotificationType}): bot token or chat id not configured, nothing sent.", notificationType);
                return false;
            }

            if (await SendPhotoCore(notificationType, message, image, target, buttons).ConfigureAwait(false))
            {
                return true;
            }

            if (buttons is { Count: > 0 }
                && await SendPhotoCore(notificationType, message, image, target, null).ConfigureAwait(false))
            {
                _logger.LogWarning("({NotificationType}): sent without buttons ({Urls}).", notificationType, string.Join(", ", buttons.Select(b => b.Url)));
                return true;
            }

            _logger.LogWarning("({NotificationType}): photo rejected, sending the message without image.", notificationType);
            return await SendMessage(notificationType, message, target, buttons).ConfigureAwait(false);
        }

        private static string BuildButtonMarkup(IReadOnlyList<InlineButton> buttons) =>
            JsonSerializer.Serialize(new
            {
                inline_keyboard = new[] { buttons.Select(b => new { text = b.Text, url = b.Url }).ToArray() }
            });

        private async Task<bool> SendTextCore(string notificationType, string message, TelegramTarget target, IReadOnlyList<InlineButton>? buttons)
        {
            var parameters = new Dictionary<string, string>
            {
                { "chat_id", target.ChatId },
                { "text", message },
                { "parse_mode", "HTML" },
                { "disable_notification", target.Silent ? "true" : "false" },
                { "link_preview_options", "{\"is_disabled\":true}" }
            };

            if (!string.IsNullOrEmpty(target.ThreadId))
            {
                parameters.Add("message_thread_id", target.ThreadId);
            }

            if (buttons is { Count: > 0 })
            {
                parameters.Add("reply_markup", BuildButtonMarkup(buttons));
            }

            using var content = new FormUrlEncodedContent(parameters);
            return await PostAsync(notificationType, "sendMessage", content, target).ConfigureAwait(false);
        }

        private async Task<bool> SendPhotoCore(string notificationType, string message, ImagePayload image, TelegramTarget target, IReadOnlyList<InlineButton>? buttons)
        {
            using var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(target.ChatId), "chat_id");
            formData.Add(new StringContent(TruncateCaption(message)), "caption");
            formData.Add(new StringContent(target.Silent ? "true" : "false"), "disable_notification");
            formData.Add(new StringContent("HTML"), "parse_mode");
            if (!string.IsNullOrEmpty(target.ThreadId))
            {
                formData.Add(new StringContent(target.ThreadId), "message_thread_id");
            }

            if (buttons is { Count: > 0 })
            {
                formData.Add(new StringContent(BuildButtonMarkup(buttons)), "reply_markup");
            }

            var imageContent = new ByteArrayContent(image.Data);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(
                image.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg");
            formData.Add(imageContent, "photo", image.FileName);

            return await PostAsync(notificationType, "sendPhoto", formData, target).ConfigureAwait(false);
        }

        private async Task<bool> PostAsync(string notificationType, string method, HttpContent content, TelegramTarget target)
        {
            var url = new Uri($"https://api.telegram.org/bot{target.BotToken}/{method}");
            try
            {
                using HttpResponseMessage response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("({NotificationType}): {Method} sent successfully.", notificationType, method);
                    return true;
                }

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError(
                    "({NotificationType}): Telegram {Method} failed with HTTP {Status}: {Body} (chatId: {ChatId}, threadId: {ThreadId})",
                    notificationType,
                    method,
                    (int)response.StatusCode,
                    body,
                    target.ChatId,
                    target.ThreadId);
                return false;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError("({NotificationType}): could not reach the Telegram API: {Message}", notificationType, ex.Message);
                return false;
            }
        }

        private static string TruncateCaption(string message)
        {
            if (message.Length <= MaxCaptionLength)
            {
                return message;
            }

            // Leave room for the closing tags added below.
            string cut = message.Substring(0, MaxCaptionLength - 60);

            // Do not leave half an HTML tag or entity at the end.
            cut = DanglingMarkupRegex().Replace(cut, string.Empty).TrimEnd() + "…";

            // Close the tags left open by the cut (e.g. the expandable plot blockquote).
            var open = new Stack<string>();
            foreach (Match m in TagRegex().Matches(cut))
            {
                string name = m.Groups[2].Value.ToLowerInvariant();
                if (m.Groups[1].Value == "/")
                {
                    if (open.Count > 0 && open.Peek() == name)
                    {
                        open.Pop();
                    }
                }
                else
                {
                    open.Push(name);
                }
            }

            var sb = new StringBuilder(cut);
            while (open.Count > 0)
            {
                sb.Append("</").Append(open.Pop()).Append('>');
            }

            return sb.ToString();
        }

        [GeneratedRegex(@"<(/?)([a-zA-Z][a-zA-Z0-9-]*)[^>]*>")]
        private static partial Regex TagRegex();

        [GeneratedRegex(@"(<[^>]*|&[a-zA-Z#0-9]*)$")]
        private static partial Regex DanglingMarkupRegex();
    }
}
