using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.TelegramNotifier.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TelegramNotifier.Telegram.Configuration;

/// <summary>
/// Settings page helpers (admin only).
/// </summary>
[Route("TelegramNotifierApi")]
[ApiController]
[Authorize(Policy = "RequiresElevation")]
public class TestNotifier : ControllerBase
{
    private readonly Sender _sender;
    private readonly ImageProvider _imageProvider;
    private readonly ILibraryManager _libraryManager;
    private readonly IServerApplicationHost _applicationHost;

    public TestNotifier(Sender sender, ImageProvider imageProvider, ILibraryManager libraryManager, IServerApplicationHost applicationHost)
    {
        _applicationHost = applicationHost;
        _sender = sender;
        _imageProvider = imageProvider;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Rows of the settings matrix with their default message.
    /// </summary>
    [HttpGet("Catalog")]
    public ActionResult<IEnumerable<object>> Catalog()
    {
        return Ok(NotificationCatalog.All.Select(d => new
        {
            key = d.Key,
            group = d.Group,
            label = d.Label,
            scope = d.Scope == NotificationScope.User ? "user" : "server",
            defaultMessage = d.DefaultMessage,
            defaultSilent = d.DefaultSilent
        }));
    }

    /// <summary>
    /// Sends a test message (with the poster of the latest movie/series, to check images too).
    /// Values typed in the page are used even if not saved yet.
    /// </summary>
    [HttpPost("Test")]
    public async Task<ActionResult<string>> Test([FromBody] TestNotifierRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var config = Plugin.Config;
        var target = new TelegramTarget(
            string.IsNullOrWhiteSpace(request.BotToken) ? config.BotToken : request.BotToken,
            string.IsNullOrWhiteSpace(request.ChatId) ? config.ChatId : request.ChatId,
            request.ThreadId ?? config.ThreadId,
            false);

        if (!target.IsValid)
        {
            return BadRequest("Bot token e chat id sono obbligatori.");
        }

        BaseItem? item = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
            ImageTypes = new[] { ImageType.Primary },
            OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
            Recursive = true,
            Limit = 1
        }) is { Count: > 0 } items ? items[0] : null;

        ImagePayload? image = item is null ? null : await _imageProvider.GetPrimaryImageAsync(item).ConfigureAwait(false);

        string text = "🎉 <b>Messaggio di prova</b>\n<i>La configurazione del bot funziona.</i>";
        if (item is not null)
        {
            text += image is null
                ? $"\n⚠️ Impossibile leggere la locandina di \"{System.Net.WebUtility.HtmlEncode(item.Name)}\": controlla i log di Jellyfin."
                : $"\n🖼️ Locandina di \"{System.Net.WebUtility.HtmlEncode(item.Name)}\" caricata correttamente.";
        }

        string? buttonUrl = NotificationFilter.BuildItemUrl(item, _applicationHost.SystemId);
        bool ok = await _sender.SendMessageWithPhoto("Test", text, image, target, buttonUrl).ConfigureAwait(false);
        return ok
            ? Ok(image is null ? "Messaggio inviato (senza immagine)." : "Messaggio con immagine inviato.")
            : BadRequest("Invio non riuscito: controlla bot token, chat id e i log di Jellyfin.");
    }
}

/// <summary>
/// Body of the test request.
/// </summary>
public class TestNotifierRequest
{
    public string? BotToken { get; set; }

    public string? ChatId { get; set; }

    public string? ThreadId { get; set; }
}
