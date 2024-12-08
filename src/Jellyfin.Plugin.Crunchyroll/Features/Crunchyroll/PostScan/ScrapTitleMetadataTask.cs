using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

public class ScrapTitleMetadataTask : IPostTitleIdSetTask, IPostMovieIdSetTask
{
    private readonly IMediator _mediator;
    private readonly ILogger<ScrapTitleMetadataTask> _logger;
    private readonly PluginConfiguration _config;

    public ScrapTitleMetadataTask(IMediator mediator, ILogger<ScrapTitleMetadataTask> logger,
        PluginConfiguration config)
    {
        _mediator = mediator;
        _logger = logger;
        _config = config;
    }
    
    public async Task RunAsync(BaseItem seriesItem, CancellationToken cancellationToken)
    {
        if (!_config.IsFeatureCrunchyrollUpdateEnabled)
        {
            return;
        }
        
        var hasId = seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var id) &&
                    !string.IsNullOrWhiteSpace(id);

        var hasSlugTitle = seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesSlugTitle, out var slugTitle) &&
                           !string.IsNullOrWhiteSpace(slugTitle);

        if (!hasId || !hasSlugTitle)
        {
            //if item has no id or slugTitle, skip this item
            _logger.LogDebug("No crunchyroll ids found on item {Name}. Skipping...", seriesItem.FileNameWithoutExtension);
            return;
        }

        var hasEpisodeId = seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId);
        var hasSeasonId = seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeasonId, out var seasonId);
        _ = await _mediator.Send(new ScrapTitleMetadataCommand
        {
            TitleId = id!,
            Language = seriesItem.GetPreferredMetadataCultureInfo(),
            MovieEpisodeId = hasEpisodeId ? episodeId : null,
            MovieSeasonId = hasSeasonId ? seasonId : null
        }, cancellationToken);
    }

    public async Task RunAsync(Movie movie, CancellationToken cancellationToken)
        => await RunAsync(seriesItem: movie, cancellationToken);
}