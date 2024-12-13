using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;
using MediaBrowser.Controller.Library;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Reviews;

public class CrunchyrollMovieReviewsService : ICrunchyrollMovieReviewsService
{
    private readonly IMediator _mediator;
    private readonly PluginConfiguration _config;

    public CrunchyrollMovieReviewsService(IMediator mediator, PluginConfiguration config)
    {
        _mediator = mediator;
        _config = config;
    }
    
    public async Task<ItemUpdateType> ScrapReviewsAsync(MediaBrowser.Controller.Entities.Movies.Movie movie, CancellationToken cancellationToken)
    {
        if (!_config.IsFeatureReviewsEnabled)
        {
            return ItemUpdateType.None;
        }
        
        var seriesId = movie.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeriesId);

        if (string.IsNullOrWhiteSpace(seriesId))
        {
            return ItemUpdateType.None;
        }

        _ = await _mediator.Send(new ExtractReviewsCommand
        {
            TitleId = seriesId,
            Language = movie.GetPreferredMetadataCultureInfo()
        }, cancellationToken);

        return ItemUpdateType.None;
    }
}