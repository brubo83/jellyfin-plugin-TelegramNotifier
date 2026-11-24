using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.TelegramNotifier.Configuration;

/// <summary>
/// Whether a notification is tied to the Jellyfin user that caused it.
/// </summary>
public enum NotificationScope
{
    /// <summary>
    /// Server-wide event (item added, plugin installed, ...): a single on/off switch.
    /// </summary>
    Server,

    /// <summary>
    /// Event caused by a Jellyfin user (playback, session, ...): can be enabled per user.
    /// </summary>
    User
}

/// <summary>
/// One row of the settings matrix.
/// </summary>
public sealed class NotificationDefinition
{
    public NotificationDefinition(string key, string group, string label, NotificationScope scope)
    {
        Key = key;
        Group = group;
        Label = label;
        Scope = scope;
    }

    public string Key { get; }

    public string Group { get; }

    public string Label { get; }

    public NotificationScope Scope { get; }

    public string DefaultMessage =>
        typeof(DefaultMessageConfiguration).GetProperty(Key)?.GetValue(null) as string ?? string.Empty;
}

/// <summary>
/// Every notification the plugin can send, in the order shown in the settings page.
/// </summary>
public static class NotificationCatalog
{
    private const string Library = "Libreria";
    private const string Playback = "Riproduzione";
    private const string Users = "Utenti e accessi";
    private const string System = "Sistema";

    public static IReadOnlyList<NotificationDefinition> All { get; } = new List<NotificationDefinition>
    {
        new("ItemAddedMovies", Library, "Aggiunto: film", NotificationScope.Server),
        new("ItemAddedSeries", Library, "Aggiunta: serie", NotificationScope.Server),
        new("ItemAddedSeasons", Library, "Aggiunta: stagione", NotificationScope.Server),
        new("ItemAddedEpisodes", Library, "Aggiunto: episodio", NotificationScope.Server),
        new("ItemAddedAlbums", Library, "Aggiunto: album", NotificationScope.Server),
        new("ItemAddedSongs", Library, "Aggiunto: brano", NotificationScope.Server),
        new("ItemAddedBooks", Library, "Aggiunto: libro", NotificationScope.Server),
        new("ItemDeletedMovies", Library, "Rimosso: film", NotificationScope.Server),
        new("ItemDeletedSeries", Library, "Rimossa: serie", NotificationScope.Server),
        new("ItemDeletedSeasons", Library, "Rimossa: stagione", NotificationScope.Server),
        new("ItemDeletedEpisodes", Library, "Rimosso: episodio", NotificationScope.Server),
        new("ItemDeletedAlbums", Library, "Rimosso: album", NotificationScope.Server),
        new("ItemDeletedSongs", Library, "Rimosso: brano", NotificationScope.Server),
        new("ItemDeletedBooks", Library, "Rimosso: libro", NotificationScope.Server),
        new("SubtitleDownloadFailure", Library, "Download sottotitoli fallito", NotificationScope.Server),

        new("PlaybackStartMovies", Playback, "Avvio: film", NotificationScope.User),
        new("PlaybackStartEpisodes", Playback, "Avvio: episodio", NotificationScope.User),
        new("PlaybackProgressMovies", Playback, "In corso: film (sconsigliato)", NotificationScope.User),
        new("PlaybackProgressEpisodes", Playback, "In corso: episodio (sconsigliato)", NotificationScope.User),
        new("PlaybackStopMovies", Playback, "Stop: film", NotificationScope.User),
        new("PlaybackStopEpisodes", Playback, "Stop: episodio", NotificationScope.User),

        new("SessionStart", Users, "Nuova sessione", NotificationScope.User),
        new("AuthenticationSuccess", Users, "Accesso riuscito", NotificationScope.User),
        new("UserLockedOut", Users, "Utente bloccato", NotificationScope.User),
        new("UserPasswordChanged", Users, "Password cambiata", NotificationScope.User),
        new("UserUpdated", Users, "Utente modificato", NotificationScope.User),
        new("AuthenticationFailure", Users, "Accesso fallito", NotificationScope.Server),
        new("UserCreated", Users, "Utente creato", NotificationScope.Server),
        new("UserDeleted", Users, "Utente eliminato", NotificationScope.Server),

        new("PendingRestart", System, "Riavvio necessario", NotificationScope.Server),
        new("TaskCompleted", System, "Attività pianificata completata", NotificationScope.Server),
        new("PluginInstalling", System, "Plugin in installazione", NotificationScope.Server),
        new("PluginInstalled", System, "Plugin installato", NotificationScope.Server),
        new("PluginUpdated", System, "Plugin aggiornato", NotificationScope.Server),
        new("PluginUninstalled", System, "Plugin disinstallato", NotificationScope.Server),
        new("PluginInstallationFailed", System, "Installazione plugin fallita", NotificationScope.Server),
        new("PluginInstallationCancelled", System, "Installazione plugin annullata", NotificationScope.Server),
    };

    public static NotificationDefinition? Find(string key) =>
        All.FirstOrDefault(d => string.Equals(d.Key, key, StringComparison.Ordinal));
}
