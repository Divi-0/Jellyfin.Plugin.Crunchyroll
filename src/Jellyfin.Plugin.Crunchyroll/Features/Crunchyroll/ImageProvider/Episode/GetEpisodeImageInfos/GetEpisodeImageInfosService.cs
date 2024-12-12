using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode.GetEpisodeImageInfos;

public class GetEpisodeImageInfosService : IGetEpisodeImageInfosService
{
    private readonly IGetEpisodeImageInfosRepository _repository;
    private readonly ILogger<GetEpisodeImageInfosService> _logger;

    public GetEpisodeImageInfosService(IGetEpisodeImageInfosRepository repository,
        ILogger<GetEpisodeImageInfosService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<RemoteImageInfo[]> GetImageInfosAsync(MediaBrowser.Controller.Entities.TV.Episode episode, CancellationToken cancellationToken)
    {
        var episodeId = episode.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.EpisodeId);

        if (string.IsNullOrWhiteSpace(episodeId))
        {
            _logger.LogDebug("Episode {Path} has no crunchyroll episode id, skipping...", episode.Path);
            return [];
        }
        
        var episodeResult = await _repository.GetEpisodeAsync(episodeId!, cancellationToken);

        if (episodeResult.IsFailed)
        {
            return [];
        }
        
        if (episodeResult.Value is null)
        {
            _logger.LogDebug("No episode for {Path} found, skipping...", episode.Path);
            return [];
        }

        var thumbnail = JsonSerializer.Deserialize<ImageSource>(episodeResult.Value.Thumbnail)!;

        return [
            new RemoteImageInfo
            {
                Url = thumbnail.Uri,
                Width = thumbnail.Width,
                Height = thumbnail.Height,
                Type = ImageType.Primary
            },
            new RemoteImageInfo
            {
                Url = thumbnail.Uri,
                Width = thumbnail.Width,
                Height = thumbnail.Height,
                Type = ImageType.Thumb
            }
        ];
    }
}