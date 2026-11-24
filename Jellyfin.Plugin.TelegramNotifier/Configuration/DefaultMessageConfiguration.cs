namespace Jellyfin.Plugin.TelegramNotifier.Configuration
{
    /// <summary>
    /// Default (Italian) message templates, one per notification key.
    /// </summary>
    public static class DefaultMessageConfiguration
    {
        public static string ItemAddedMovies { get; } = "🆕🎬 <b>{item.Name}</b> ({item.ProductionYear})\n<i>Film aggiunto alla libreria</i>{?item.Genres}\n🎭 {item.Genres}{/item.Genres}{?item.CommunityRating}\n⭐ {item.CommunityRating}{/item.CommunityRating}{?item.Overview}\n<blockquote expandable>{item.Overview}</blockquote>{/item.Overview}";

        public static string ItemAddedSeries { get; } = "🆕📺 <b>{serie.Name}</b> ({item.ProductionYear})\n<i>Serie aggiunta alla libreria</i>{?item.Genres}\n🎭 {item.Genres}{/item.Genres}{?item.CommunityRating}\n⭐ {item.CommunityRating}{/item.CommunityRating}{?item.Overview}\n<blockquote expandable>{item.Overview}</blockquote>{/item.Overview}";

        public static string ItemAddedSeasons { get; } = "🆕📺 <b>{season.Series.Name}</b>\n<i>Stagione {seasonNumber} aggiunta alla libreria</i>";

        public static string ItemAddedEpisodes { get; } = "🆕📺 <b>{episode.Series.Name}</b>\n<i>S{eSeasonNumber}E{episodeNumber} · {item.Name}</i>\nEpisodio aggiunto alla libreria{?item.Overview}\n<blockquote expandable>{item.Overview}</blockquote>{/item.Overview}";

        public static string ItemAddedAlbums { get; } = "🆕🎵 <b>{album.Name}</b> ({item.ProductionYear})\n<i>Album aggiunto alla libreria</i>";

        public static string ItemAddedSongs { get; } = "🆕🎵 <b>{audio.Name}</b> ({item.ProductionYear})\n<i>Brano aggiunto alla libreria</i>";

        public static string ItemAddedBooks { get; } = "🆕📖 <b>{item.Name}</b>\n<i>Libro aggiunto alla libreria</i>{?item.Overview}\n<blockquote expandable>{item.Overview}</blockquote>{/item.Overview}";

        public static string ItemDeletedMovies { get; } = "🗑️🎬 <b>{item.Name}</b> ({item.ProductionYear})\n<i>Film rimosso dalla libreria</i>";

        public static string ItemDeletedSeries { get; } = "🗑️📺 <b>{serie.Name}</b> ({item.ProductionYear})\n<i>Serie rimossa dalla libreria</i>";

        public static string ItemDeletedSeasons { get; } = "🗑️📺 <b>{season.Series.Name}</b>\n<i>Stagione {seasonNumber} rimossa dalla libreria</i>";

        public static string ItemDeletedEpisodes { get; } = "🗑️📺 <b>{episode.Series.Name}</b>\n<i>S{eSeasonNumber}E{episodeNumber} · {item.Name}</i>\nEpisodio rimosso dalla libreria";

        public static string ItemDeletedAlbums { get; } = "🗑️🎵 <b>{album.Name}</b> ({item.ProductionYear})\n<i>Album rimosso dalla libreria</i>";

        public static string ItemDeletedSongs { get; } = "🗑️🎵 <b>{audio.Name}</b> ({item.ProductionYear})\n<i>Brano rimosso dalla libreria</i>";

        public static string ItemDeletedBooks { get; } = "🗑️📖 <b>{item.Name}</b>\n<i>Libro rimosso dalla libreria</i>";

        public static string PlaybackStartMovies { get; } = "▶️ <b>{eventArgs.Item.Name}</b> ({eventArgs.Item.ProductionYear})\n👤 {eventArgs.Users[0].Username}\n<i>{playback.Device}{?duration} · {duration}{/duration}{?eventArgs.Item.Genres} · {eventArgs.Item.Genres}{/eventArgs.Item.Genres}{?item.CommunityRating} · ⭐ {item.CommunityRating}{/item.CommunityRating}</i>{?playback.Method}\n📺 {?playback.Quality}{playback.Quality} · {/playback.Quality}{playback.Method}{/playback.Method}{?playback.Progress}\n⏩ Riprende dal {playback.Progress}{/playback.Progress}{?eventArgs.Item.Overview}\n<blockquote expandable>{eventArgs.Item.Overview}</blockquote>{/eventArgs.Item.Overview}";

        public static string PlaybackStartEpisodes { get; } = "▶️ <b>{eventArgs.Item.Series.Name}</b>\n<i>S{playbackSeasonNumber}E{playbackEpisodeNumber} · {eventArgs.Item.Name}</i>\n👤 {eventArgs.Users[0].Username}\n<i>{playback.Device}{?duration} · {duration}{/duration}{?eventArgs.Item.Series.Genres} · {eventArgs.Item.Series.Genres}{/eventArgs.Item.Series.Genres}{?item.CommunityRating} · ⭐ {item.CommunityRating}{/item.CommunityRating}</i>{?playback.Method}\n📺 {?playback.Quality}{playback.Quality} · {/playback.Quality}{playback.Method}{/playback.Method}{?playback.Progress}\n⏩ Riprende dal {playback.Progress}{/playback.Progress}{?eventArgs.Item.Overview}\n<blockquote expandable>{eventArgs.Item.Overview}</blockquote>{/eventArgs.Item.Overview}";

        public static string PlaybackProgressMovies { get; } = "⏯️ <b>{eventArgs.Item.Name}</b> ({eventArgs.Item.ProductionYear})\n👤 {eventArgs.Users[0].Username} sta ancora guardando\n<i>{playback.Device}</i>";

        public static string PlaybackProgressEpisodes { get; } = "⏯️ <b>{eventArgs.Item.Series.Name}</b>\n<i>S{playbackSeasonNumber}E{playbackEpisodeNumber} · {eventArgs.Item.Name}</i>\n👤 {eventArgs.Users[0].Username} sta ancora guardando\n<i>{playback.Device}</i>";

        public static string PlaybackStopMovies { get; } = "⏹️ <b>{eventArgs.Item.Name}</b> ({eventArgs.Item.ProductionYear})\n👤 {eventArgs.Users[0].Username} ha smesso di guardare\n<i>{playback.Device}</i>";

        public static string PlaybackStopEpisodes { get; } = "⏹️ <b>{eventArgs.Item.Series.Name}</b>\n<i>S{playbackSeasonNumber}E{playbackEpisodeNumber} · {eventArgs.Item.Name}</i>\n👤 {eventArgs.Users[0].Username} ha smesso di guardare\n<i>{playback.Device}</i>";

        public static string SubtitleDownloadFailure { get; } = "🚫 <b>Sottotitoli non scaricati</b>\n<i>{eventArgs.Item.Name}</i>";

        public static string AuthenticationFailure { get; } = "🔒 <b>Accesso fallito</b> per {eventArgs.Argument.Username}\n<i>{eventArgs.Argument.DeviceName}</i>";

        public static string AuthenticationSuccess { get; } = "🔓 <b>{eventArgs.Argument.User.Name}</b> ha effettuato l'accesso\n<i>{eventArgs.Argument.SessionInfo.DeviceName}</i>";

        public static string SessionStart { get; } = "🟢 <b>{eventArgs.Argument.UserName}</b> ha aperto una sessione\n<i>{eventArgs.Argument.Client} · {eventArgs.Argument.DeviceName}</i>";

        public static string PendingRestart { get; } = "🔄 <b>Jellyfin richiede un riavvio</b>";

        public static string TaskCompleted { get; } = "🧰 <b>{eventArgs.Task.Name}</b> completato\n<i>{eventArgs.Task.Category}</i>";

        public static string PluginInstallationCancelled { get; } = "🔴 <b>{eventArgs.Argument.Name}</b>: installazione annullata\n<i>versione {eventArgs.Argument.Version}</i>";

        public static string PluginInstallationFailed { get; } = "🔴 <b>{eventArgs.InstallationInfo}</b>: installazione fallita\n<i>{eventArgs.Exception}</i>";

        public static string PluginInstalled { get; } = "🧩 <b>{eventArgs.Argument.Name}</b> installato\n<i>versione {eventArgs.Argument.Version}</i>";

        public static string PluginInstalling { get; } = "🧩 <b>{eventArgs.Argument.Name}</b> in installazione\n<i>versione {eventArgs.Argument.Version}</i>";

        public static string PluginUninstalled { get; } = "🧩 <b>{eventArgs.Argument.Name}</b> disinstallato";

        public static string PluginUpdated { get; } = "🧩 <b>{eventArgs.Argument.Name}</b> aggiornato\n<i>versione {eventArgs.Argument.Version}</i>";

        public static string UserCreated { get; } = "👤 <b>{eventArgs.Argument.Username}</b>\n<i>Utente creato</i>";

        public static string UserDeleted { get; } = "🗑️ <b>{eventArgs.Argument.Username}</b>\n<i>Utente eliminato</i>";

        public static string UserLockedOut { get; } = "🔒 <b>{eventArgs.Argument.Username}</b>\n<i>Utente bloccato</i>";

        public static string UserPasswordChanged { get; } = "🔑 <b>{eventArgs.Argument.Username}</b>\n<i>Password cambiata</i>";

        public static string UserUpdated { get; } = "👤 <b>{eventArgs.Argument.Username}</b>\n<i>Utente modificato</i>";
    }
}
