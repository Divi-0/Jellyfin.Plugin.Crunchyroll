using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie.GetMovieImageInfos;

public class GetMovieImageInfosService : IGetMovieImageInfosService
{
    private const string BackdropBaseUrl = "https://imgsrv.crunchyroll.com/cdn-cgi/image";

    private readonly IGetMovieImageInfosRepository _repository;
    private readonly ILogger<GetMovieImageInfosService> _logger;
    private readonly PluginConfiguration _config;

    public GetMovieImageInfosService(IGetMovieImageInfosRepository repository,
        ILogger<GetMovieImageInfosService> logger, PluginConfiguration config)
    {
        _repository = repository;
        _logger = logger;
        _config = config;
    }

    public async Task<RemoteImageInfo[]> GetImageInfosAsync(MediaBrowser.Controller.Entities.Movies.Movie movie, CancellationToken cancellationToken)
    {
        var imageInfos = new List<RemoteImageInfo>();

        var thumbImageInfo = await GetThumbImageInfoAsync(movie, cancellationToken);
        if (thumbImageInfo is not null)
        {
            imageInfos.Add(thumbImageInfo);
        }

        var seriesId = movie.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeriesId);
        if (!string.IsNullOrWhiteSpace(seriesId))
        {
            imageInfos.Add(GetBackdropImageInfo(seriesId));
        }

        return imageInfos.ToArray();
    }

    private async Task<RemoteImageInfo?> GetThumbImageInfoAsync(MediaBrowser.Controller.Entities.Movies.Movie movie, CancellationToken cancellationToken)
    {
        var episodeId = movie.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.EpisodeId);

        if (string.IsNullOrWhiteSpace(episodeId))
        {
            return null;
        }

        var thumbnailResult = await _repository.GetEpisodeThumbnailAsync(episodeId, cancellationToken);

        if (thumbnailResult.IsFailed)
        {
            return null;
        }

        if (thumbnailResult.Value is null)
        {
            _logger.LogDebug("No episode thumbnail for episode {EpisodeId} found", episodeId);
            return null;
        }

        return new RemoteImageInfo
        {
            Url = thumbnailResult.Value.Uri,
            Width = thumbnailResult.Value.Width,
            Height = thumbnailResult.Value.Height,
            Type = ImageType.Thumb
        };
    }

    private RemoteImageInfo GetBackdropImageInfo(string seriesId)
    {
        var (width, quality) = _config.MovieBackdropImageWidth switch
        {
            1920 => (1920, 85),
            _ => (3840, 100)
        };

        return new RemoteImageInfo
        {
            Url = $"{BackdropBaseUrl}/fit=cover,format=auto,quality={quality},width={width}/keyart/{seriesId}-backdrop_wide",
            Width = width,
            Type = ImageType.Backdrop
        };
    }
}