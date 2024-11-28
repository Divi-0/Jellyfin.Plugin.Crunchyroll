using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

public sealed class ExtractCommentsTask : IPostEpisodeIdSetTask, IPostMovieIdSetTask
{
    private readonly IMediator _mediator;
    private readonly PluginConfiguration _config;
    private readonly ILogger<ExtractCommentsTask> _logger;

    public ExtractCommentsTask(IMediator mediator, PluginConfiguration config, ILogger<ExtractCommentsTask> logger)
    {
        _mediator = mediator;
        _config = config;
        _logger = logger;
    }
    
    public async Task RunAsync(BaseItem episodeItem, CancellationToken cancellationToken)
    {
        if (!_config.IsWaybackMachineEnabled)
        {
            return;
        }
        
        var hasId = episodeItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var id) &&
                    !string.IsNullOrWhiteSpace(id);

        var hasSlugTitle = episodeItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeSlugTitle, out var slugTitle) &&
                           !string.IsNullOrWhiteSpace(slugTitle);

        if (!hasId || !hasSlugTitle)
        {
            //if item has no id or slugTitle, skip this item
            _logger.LogDebug("No crunchyroll ids found on episode item with path {Path}. Skipping...", episodeItem.Path);
            return;
        }

        var language = episodeItem.GetPreferredMetadataCultureInfo();
        _ = await _mediator.Send(new ExtractCommentsCommand(id!, slugTitle!, language), cancellationToken);
    }

    public async Task RunAsync(Movie movie, CancellationToken cancellationToken)
    {
        await RunAsync(episodeItem: movie, cancellationToken);
    }
}