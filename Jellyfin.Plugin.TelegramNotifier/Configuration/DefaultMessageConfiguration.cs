namespace Jellyfin.Plugin.TelegramNotifier.Configuration
{
    /// <summary>
    /// Default (Italian) message templates, one per notification key.
    /// </summary>
    public static class DefaultMessageConfiguration
    {
        public static string ItemAddedMovies { get; } = "🎬 {item.Name} ({item.ProductionYear})\n      aggiunto alla libreria";

        public static string ItemAddedSeries { get; } = "📺 [Serie] {serie.Name} ({item.ProductionYear}) aggiunta alla libreria";

        public static string ItemAddedSeasons { get; } = "📺 {season.Series.Name} ({item.ProductionYear})\n      Stagione {seasonNumber} aggiunta alla libreria";

        public static string ItemAddedEpisodes { get; } = "📺 {episode.Series.Name} ({item.ProductionYear})\n      S{eSeasonNumber} - E{episodeNumber}\n      '{item.Name}' aggiunto alla libreria";

        public static string ItemAddedAlbums { get; } = "🎵 [Album] {album.Name} ({item.ProductionYear}) aggiunto alla libreria";

        public static string ItemAddedSongs { get; } = "🎵 [Audio] {audio.Name} ({item.ProductionYear}) aggiunto alla libreria";

        public static string ItemAddedBooks { get; } = "📖 [Libro] {item.Name} aggiunto alla libreria";

        public static string ItemDeletedMovies { get; } = "🗑️🎬 {item.Name} ({item.ProductionYear})\n      rimosso dalla libreria";

        public static string ItemDeletedSeries { get; } = "🗑️📺 [Serie] {serie.Name} ({item.ProductionYear}) rimossa dalla libreria";

        public static string ItemDeletedSeasons { get; } = "🗑️📺 {season.Series.Name} ({item.ProductionYear})\n      Stagione {seasonNumber} rimossa dalla libreria";

        public static string ItemDeletedEpisodes { get; } = "🗑️📺 {episode.Series.Name} ({item.ProductionYear})\n      S{eSeasonNumber} - E{episodeNumber}\n      '{item.Name}' rimosso dalla libreria";

        public static string ItemDeletedAlbums { get; } = "🗑️🎵 [Album] {album.Name} ({item.ProductionYear}) rimosso dalla libreria";

        public static string ItemDeletedSongs { get; } = "🗑️🎵 [Audio] {audio.Name} ({item.ProductionYear}) rimosso dalla libreria";

        public static string ItemDeletedBooks { get; } = "🗑️📖 [Libro] {item.Name} rimosso dalla libreria";

        public static string PlaybackStartMovies { get; } = "👤 {eventArgs.Users[0].Username} sta guardando su {eventArgs.DeviceName}:\n🎬 {eventArgs.Item.Name} ({eventArgs.Item.ProductionYear})\n📺 [{eventArgs.Item.MediaType}] {eventArgs.Item.Genres}\n🕒 {duration}";

        public static string PlaybackStartEpisodes { get; } = "👤 {eventArgs.Users[0].Username} sta guardando su {eventArgs.DeviceName}:\n🎬 {eventArgs.Item.Series.Name} ({eventArgs.Item.ProductionYear})\n      S{playbackSeasonNumber} - E{playbackEpisodeNumber}\n      '{eventArgs.Item.Name}'\n📺 [{eventArgs.Item.MediaType}] {eventArgs.Item.Genres}\n🕒 {duration}";

        public static string PlaybackProgressMovies { get; } = "👤 {eventArgs.Users[0].Username} sta ancora guardando su {eventArgs.DeviceName}:\n🎬 {eventArgs.Item.Name} ({eventArgs.Item.ProductionYear})";

        public static string PlaybackProgressEpisodes { get; } = "👤 {eventArgs.Users[0].Username} sta ancora guardando su {eventArgs.DeviceName}:\n🎬 {eventArgs.Item.Series.Name} ({eventArgs.Item.ProductionYear})\n      S{playbackSeasonNumber} - E{playbackEpisodeNumber}\n      '{eventArgs.Item.Name}'";

        public static string PlaybackStopMovies { get; } = "👤 {eventArgs.Users[0].Username} ha smesso di guardare:\n🎬 {eventArgs.Item.Name} ({eventArgs.Item.ProductionYear})";

        public static string PlaybackStopEpisodes { get; } = "👤 {eventArgs.Users[0].Username} ha smesso di guardare:\n🎬 {eventArgs.Item.Series.Name} ({eventArgs.Item.ProductionYear})\n      S{playbackSeasonNumber} - E{playbackEpisodeNumber}\n      '{eventArgs.Item.Name}'";

        public static string SubtitleDownloadFailure { get; } = "🚫 Download dei sottotitoli fallito per {eventArgs.Item.Name}";

        public static string AuthenticationFailure { get; } = "🔒 Autenticazione fallita per l'utente {eventArgs.Argument.Username} su {eventArgs.Argument.DeviceName}";

        public static string AuthenticationSuccess { get; } = "🔓 Autenticazione avvenuta per l'utente {eventArgs.Argument.User.Name} su {eventArgs.Argument.SessionInfo.DeviceName}";

        public static string SessionStart { get; } = "👤 {eventArgs.Argument.UserName} ha iniziato una sessione su:\n💻 {eventArgs.Argument.Client} ({eventArgs.Argument.DeviceName})";

        public static string PendingRestart { get; } = "🔄 Jellyfin richiede un riavvio.";

        public static string TaskCompleted { get; } = "🧰 Task {eventArgs.Task.Name} completato: {eventArgs.Task.CurrentProgress}%\n🗒️ ({eventArgs.Task.Category}) {eventArgs.Task.Description}";

        public static string PluginInstallationCancelled { get; } = "🔴 Installazione del plugin {eventArgs.Argument.Name} annullata (versione {eventArgs.Argument.Version})";

        public static string PluginInstallationFailed { get; } = "🔴 Installazione del plugin {eventArgs.InstallationInfo} fallita (versione {eventArgs.VersionInfo}):\n{eventArgs.Exception}";

        public static string PluginInstalled { get; } = "🚧 Plugin {eventArgs.Argument.Name} installato (versione {eventArgs.Argument.Version})";

        public static string PluginInstalling { get; } = "🚧 Installazione del plugin {eventArgs.Argument.Name} in corso (versione {eventArgs.Argument.Version})";

        public static string PluginUninstalled { get; } = "🚧 Plugin {eventArgs.Argument.Name} disinstallato";

        public static string PluginUpdated { get; } = "🚧 {eventArgs.Argument.Name} plugin aggiornato alla versione {eventArgs.Argument.Version}:\n🗒️ {eventArgs.Argument.Changelog}";

        public static string UserCreated { get; } = "👤 Utente {eventArgs.Argument.Username} creato.";

        public static string UserDeleted { get; } = "🗑️ Utente {eventArgs.Argument.Username} eliminato.";

        public static string UserLockedOut { get; } = "👤🔒 Utente {eventArgs.Argument.Username} bloccato";

        public static string UserPasswordChanged { get; } = "👤 L'utente {eventArgs.Argument.Username} ha cambiato la propria password.";

        public static string UserUpdated { get; } = "👤 L'utente {eventArgs.Argument.Username} è stato aggiornato";
    }
}
