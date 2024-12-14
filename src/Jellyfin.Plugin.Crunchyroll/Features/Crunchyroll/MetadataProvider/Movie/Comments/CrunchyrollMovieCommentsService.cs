using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;
using MediaBrowser.Controller.Library;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Comments;

public class CrunchyrollMovieCommentsService : ICrunchyrollMovieCommentsService
{
    private readonly IMediator _mediator;
    private readonly PluginConfiguration _config;

    public CrunchyrollMovieCommentsService(IMediator mediator,
        PluginConfiguration config)
    {
        _mediator = mediator;
        _config = config;
    }
    
    public async Task<ItemUpdateType> ScrapCommentsAsync(MediaBrowser.Controller.Entities.Movies.Movie movie, CancellationToken cancellationToken)
    {
        if (!_config.IsFeatureCommentsEnabled)
        {
            return ItemUpdateType.None;
        }
        
        var episodeId = movie.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.EpisodeId);

        if (string.IsNullOrWhiteSpace(episodeId))
        {
            return ItemUpdateType.None;
        }

        _ = await _mediator.Send(new ExtractCommentsCommand(episodeId, movie.GetPreferredMetadataCultureInfo()), 
            cancellationToken);

        return ItemUpdateType.None;
    }
}