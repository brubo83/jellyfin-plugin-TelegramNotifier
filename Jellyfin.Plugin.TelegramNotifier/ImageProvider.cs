using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TelegramNotifier
{
    /// <summary>
    /// An image ready to be uploaded to Telegram.
    /// </summary>
    public sealed class ImagePayload
    {
        public ImagePayload(byte[] data, string fileName)
        {
            Data = data;
            FileName = fileName;
        }

        public byte[] Data { get; }

        public string FileName { get; }
    }

    /// <summary>
    /// Gets the poster of an item without going through the public URL of the server.
    /// <para>
    /// The original plugin downloaded the image from "http://" + ServerUrl. Behind a reverse proxy
    /// (nginx-proxy-manager and similar) that URL is the public domain: from inside the Jellyfin container it
    /// is often unreachable (no hairpin NAT, Docker DNS), gets redirected to HTTPS, or goes through the
    /// HTTP proxy configured for Telegram. When the download failed the whole notification was dropped.
    /// </para>
    /// <para>
    /// Here the image is read straight from disk, falling back to Jellyfin's own local API address, and
    /// only then to the configured ServerUrl. The bytes are then uploaded to Telegram, so Telegram never
    /// needs to reach the server.
    /// </para>
    /// </summary>
    public sealed class ImageProvider : IDisposable
    {
        // Telegram accepts photos up to 10 MB; stay well below.
        private const long MaxPhotoBytes = 9 * 1024 * 1024;
        private const int MaxWidth = 1280;

        private static readonly HashSet<string> TelegramFriendlyExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png"
        };

        private readonly ILogger<ImageProvider> _logger;
        private readonly IServerApplicationHost _applicationHost;
        private readonly HttpClient _localHttpClient;

        public ImageProvider(ILogger<ImageProvider> logger, IServerApplicationHost applicationHost)
        {
            _logger = logger;
            _applicationHost = applicationHost;

            // Local requests must never go through the HTTP proxy configured for Telegram.
#pragma warning disable CA2000, CA5399 // Handler is disposed by HttpClient; local, plain-HTTP loopback traffic.
            var handler = new HttpClientHandler { UseProxy = false, AllowAutoRedirect = true };
            _localHttpClient = new HttpClient(handler, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(15) };
#pragma warning restore CA2000, CA5399
        }

        public void Dispose() => _localHttpClient.Dispose();

        /// <summary>
        /// Returns the item to take the image from (series poster for episodes if configured).
        /// </summary>
        public static BaseItem ResolveImageItem(BaseItem item)
        {
            if (Plugin.Config.KeepSerieImage && item is MediaBrowser.Controller.Entities.TV.Episode { Series: not null } episode)
            {
                return episode.Series;
            }

            return item;
        }

        public async Task<ImagePayload?> GetPrimaryImageAsync(BaseItem item, CancellationToken cancellationToken = default)
        {
            ItemImageInfo? info = null;
            try
            {
                info = item.GetImageInfo(ImageType.Primary, 0);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                _logger.LogDebug(ex, "No primary image info for {ItemName}", item.Name);
            }

            if (info is null && item is MediaBrowser.Controller.Entities.TV.Episode { Series: not null } ep)
            {
                // Episodes without a thumbnail: use the series poster.
                item = ep.Series;
                info = item.GetImageInfo(ImageType.Primary, 0);
            }

            if (info is null)
            {
                _logger.LogDebug("{ItemName} has no primary image, sending text only", item.Name);
                return null;
            }

            // 1. Straight from disk: no network at all.
            ImagePayload? fromDisk = await TryReadLocalFileAsync(info, strict: true, cancellationToken).ConfigureAwait(false);
            if (fromDisk is not null)
            {
                return fromDisk;
            }

            // 2. Through Jellyfin's own API (resized JPEG) using the local address, then the configured URL.
            foreach (string baseUrl in GetLocalBaseUrls())
            {
                ImagePayload? fromApi = await TryDownloadAsync(baseUrl, item, cancellationToken).ConfigureAwait(false);
                if (fromApi is not null)
                {
                    return fromApi;
                }
            }

            // 3. Last chance: the local file even if it is a format/size Telegram may not like.
            return await TryReadLocalFileAsync(info, strict: false, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImagePayload?> TryReadLocalFileAsync(ItemImageInfo info, bool strict, CancellationToken cancellationToken)
        {
            try
            {
                if (!info.IsLocalFile || string.IsNullOrEmpty(info.Path) || !File.Exists(info.Path))
                {
                    return null;
                }

                string extension = Path.GetExtension(info.Path);
                long length = new FileInfo(info.Path).Length;
                if (length == 0 || length > MaxPhotoBytes)
                {
                    return null;
                }

                if (strict && !TelegramFriendlyExtensions.Contains(extension))
                {
                    return null;
                }

                byte[] data = await File.ReadAllBytesAsync(info.Path, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Image read from disk: {Path}", info.Path);
                return new ImagePayload(data, "image" + extension.ToLowerInvariant());
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning("Could not read image file {Path}: {Message}", info.Path, ex.Message);
                return null;
            }
        }

        private async Task<ImagePayload?> TryDownloadAsync(string baseUrl, BaseItem item, CancellationToken cancellationToken)
        {
            string url = $"{baseUrl.TrimEnd('/')}/Items/{item.Id:N}/Images/Primary?maxWidth={MaxWidth}&quality=90&format=Jpg";
            try
            {
                using HttpResponseMessage response = await _localHttpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Image download from {Url} failed: HTTP {Status}", url, (int)response.StatusCode);
                    return null;
                }

                string? mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType is not null && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Image download from {Url} returned {MediaType} instead of an image", url, mediaType);
                    return null;
                }

                byte[] data = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                if (data.Length == 0 || data.Length > MaxPhotoBytes)
                {
                    return null;
                }

                _logger.LogDebug("Image downloaded from {Url}", url);
                return new ImagePayload(data, "image.jpg");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
            {
                _logger.LogWarning("Image download from {Url} failed: {Message}", url, ex.Message);
                return null;
            }
        }

        private IEnumerable<string> GetLocalBaseUrls()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string? local = null;
            try
            {
                local = _applicationHost.GetApiUrlForLocalAccess(null, false);
            }
            catch (Exception ex) when (ex is InvalidOperationException or NullReferenceException)
            {
                _logger.LogDebug(ex, "GetApiUrlForLocalAccess failed");
            }

            if (!string.IsNullOrWhiteSpace(local) && seen.Add(local))
            {
                yield return local;
            }

            string configured = Plugin.Config.ServerUrl?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(configured))
            {
                if (!configured.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    && !configured.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    configured = "http://" + configured;
                }

                if (seen.Add(configured))
                {
                    yield return configured;
                }
            }
        }
    }
}
