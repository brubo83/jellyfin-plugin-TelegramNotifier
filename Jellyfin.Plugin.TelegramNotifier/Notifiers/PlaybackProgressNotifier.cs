using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.TelegramNotifier.Notifiers;

public class PlaybackProgressNotifier : IEventConsumer<PlaybackProgressEventArgs>
{
    // Playback key -> time of the last notification (or of the first progress event seen).
    private static readonly ConcurrentDictionary<string, DateTime> _lastSent = new();
    private readonly NotificationFilter _notificationFilter;

    public PlaybackProgressNotifier(NotificationFilter notificationFilter)
    {
        _notificationFilter = notificationFilter;
    }

    public async Task OnEvent(PlaybackProgressEventArgs eventArgs)
    {
        if (eventArgs == null)
        {
            throw new ArgumentNullException(nameof(eventArgs));
        }

        if (eventArgs.Item is null || eventArgs.Users.Count == 0 || eventArgs.Item.IsThemeMedia)
        {
            return;
        }

        if (eventArgs.Item.GetType() == typeof(MediaBrowser.Controller.Entities.Audio.Audio))
        {
            return;
        }

        string subtype = "PlaybackProgressMovies";

        switch (eventArgs.Item)
        {
            case Episode episode:
                subtype = "PlaybackProgressEpisodes";
                break;
        }

        /* string message = $"👤 {eventArgs.Users[0].Username} is still watching on {eventArgs.DeviceName}:\n" +
                         $"🎬 {eventArgs.Item.Name} ({eventArgs.Item.ProductionYear})";

        switch (eventArgs.Item)
        {
            case Episode episode:
                string seasonNumber = episode.Season.IndexNumber.HasValue ? episode.Season.IndexNumber.Value.ToString("00", CultureInfo.InvariantCulture) : "00";
                string episodeNumber = episode.IndexNumber.HasValue ? episode.IndexNumber.Value.ToString("00", CultureInfo.InvariantCulture) : "00";

                message = $"👤 {eventArgs.Users[0].Username} is still watching on {eventArgs.DeviceName}:\n" +
                          $"🎬 {episode.Series.Name} ({eventArgs.Item.ProductionYear})\n" +
                          $"      S{seasonNumber} - E{episodeNumber}\n" +
                          $"      '{eventArgs.Item.Name}'";
                break;
        } */

        var userIds = eventArgs.Users.Select(u => NotificationFilter.NormalizeUserId(u.Id)).ToList();

        // Jellyfin reports progress every ~10 s: only notify once per interval for the same playback,
        // and not right at the start (that is what PlaybackStart is for).
        if (!ShouldNotify($"{eventArgs.DeviceId}|{eventArgs.Item.Id:N}|{userIds[0]}"))
        {
            return;
        }

        await _notificationFilter.Filter(NotificationFilter.NotificationType.PlaybackProgress, eventArgs, userIds: userIds, subtype: subtype, linkItem: eventArgs.Item).ConfigureAwait(false);
    }

    private static bool ShouldNotify(string key)
    {
        DateTime now = DateTime.UtcNow;
        int minutes = Plugin.Config.PlaybackProgressIntervalMinutes;
        TimeSpan interval = TimeSpan.FromMinutes(minutes > 0 ? minutes : 15);

        // Forget playbacks that stopped long ago.
        foreach (var entry in _lastSent)
        {
            if (now - entry.Value > interval + TimeSpan.FromHours(6))
            {
                _lastSent.TryRemove(entry.Key, out _);
            }
        }

        if (!_lastSent.TryGetValue(key, out DateTime last) || now - last > interval + interval)
        {
            // First progress event of this playback (or after a long pause): start counting from now.
            _lastSent[key] = now;
            return false;
        }

        if (now - last < interval)
        {
            return false;
        }

        _lastSent[key] = now;
        return true;
    }
}
