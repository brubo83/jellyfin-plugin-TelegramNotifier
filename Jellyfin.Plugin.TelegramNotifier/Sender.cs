using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

        public async Task<bool> SendMessage(string notificationType, string message, TelegramTarget target)
        {
            if (!target.IsValid)
            {
                _logger.LogWarning("({NotificationType}): bot token or chat id not configured, nothing sent.", notificationType);
                return false;
            }

            var parameters = new Dictionary<string, string>
            {
                { "chat_id", target.ChatId },
                { "text", message },
                { "parse_mode", "HTML" },
                { "disable_notification", target.Silent ? "true" : "false" }
            };

            if (!string.IsNullOrEmpty(target.ThreadId))
            {
                parameters.Add("message_thread_id", target.ThreadId);
            }

            using var content = new FormUrlEncodedContent(parameters);
            return await PostAsync(notificationType, "sendMessage", content, target).ConfigureAwait(false);
        }

        /// <summary>
        /// Uploads the image bytes with the message as caption. If the photo cannot be sent for any
        /// reason, the message is sent as plain text so the notification is never lost.
        /// </summary>
        public async Task<bool> SendMessageWithPhoto(string notificationType, string message, ImagePayload? image, TelegramTarget target)
        {
            if (image is null)
            {
                return await SendMessage(notificationType, message, target).ConfigureAwait(false);
            }

            if (!target.IsValid)
            {
                _logger.LogWarning("({NotificationType}): bot token or chat id not configured, nothing sent.", notificationType);
                return false;
            }

            using (var formData = new MultipartFormDataContent())
            {
                formData.Add(new StringContent(target.ChatId), "chat_id");
                formData.Add(new StringContent(TruncateCaption(message)), "caption");
                formData.Add(new StringContent(target.Silent ? "true" : "false"), "disable_notification");
                formData.Add(new StringContent("HTML"), "parse_mode");
                if (!string.IsNullOrEmpty(target.ThreadId))
                {
                    formData.Add(new StringContent(target.ThreadId), "message_thread_id");
                }

                var imageContent = new ByteArrayContent(image.Data);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(
                    image.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg");
                formData.Add(imageContent, "photo", image.FileName);

                if (await PostAsync(notificationType, "sendPhoto", formData, target).ConfigureAwait(false))
                {
                    return true;
                }
            }

            _logger.LogWarning("({NotificationType}): photo rejected, sending the message without image.", notificationType);
            return await SendMessage(notificationType, message, target).ConfigureAwait(false);
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

            string cut = message.Substring(0, MaxCaptionLength - 1);

            // Do not leave half an HTML tag or entity at the end.
            cut = DanglingMarkupRegex().Replace(cut, string.Empty);
            return cut.TrimEnd() + "…";
        }

        [GeneratedRegex(@"(<[^>]*|&[a-zA-Z#0-9]*)$")]
        private static partial Regex DanglingMarkupRegex();
    }
}
