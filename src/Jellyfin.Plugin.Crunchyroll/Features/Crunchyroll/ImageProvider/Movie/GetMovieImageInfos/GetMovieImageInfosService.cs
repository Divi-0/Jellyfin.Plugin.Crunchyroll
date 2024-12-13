using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie.GetMovieImageInfos;

public class GetMovieImageInfosService : IGetMovieImageInfosService
{
    private readonly IGetMovieImageInfosRepository _repository;
    private readonly ILogger<GetMovieImageInfosService> _logger;

    public GetMovieImageInfosService(IGetMovieImageInfosRepository repository, 
        ILogger<GetMovieImageInfosService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<RemoteImageInfo[]> GetImageInfosAsync(MediaBrowser.Controller.Entities.Movies.Movie movie, CancellationToken cancellationToken)
    {
        var episodeId = movie.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.EpisodeId);

        if (string.IsNullOrWhiteSpace(episodeId))
        {
            return [];
        }

        var thumbnailResult = await _repository.GetEpisodeThumbnailAsync(episodeId, cancellationToken);

        if (thumbnailResult.IsFailed)
        {
            return [];
        }

        if (thumbnailResult.Value is null)
        {
            _logger.LogDebug("No episode thumbnail for episode {EpisodeId} found", episodeId);
            return [];
        }
        
        return [
            new RemoteImageInfo
            {
                Url = thumbnailResult.Value.Uri,
                Width = thumbnailResult.Value.Width,
                Height = thumbnailResult.Value.Height,
                Type = ImageType.Thumb
            }
        ];
    }
}