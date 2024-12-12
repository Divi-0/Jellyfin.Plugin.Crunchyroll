using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series.GetSeriesImageInfos;

public class GetSeriesImageInfosService : IGetSeriesImageInfosService
{
    private readonly IGetSeriesImageInfosRepository _repository;
    private readonly ILogger<GetSeriesImageInfosService> _logger;

    public GetSeriesImageInfosService(IGetSeriesImageInfosRepository repository,
        ILogger<GetSeriesImageInfosService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<RemoteImageInfo[]> GetImageInfosAsync(MediaBrowser.Controller.Entities.TV.Series series, CancellationToken cancellationToken)
    {
        var seriesId = series.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeriesId);

        if (string.IsNullOrWhiteSpace(seriesId))
        {
            _logger.LogDebug("Series {Path} has no crunchyroll series id, skipping...", series.Path);
            return [];
        }
        
        var titleMetadataResult = await _repository.GetTitleMetadataAsync(seriesId!, cancellationToken);

        if (titleMetadataResult.IsFailed)
        {
            return [];
        }
        
        if (titleMetadataResult.Value is null)
        {
            _logger.LogDebug("No titlemetadata for series {Path} found, skipping...", series.Path);
            return [];
        }

        var posterTall = JsonSerializer.Deserialize<ImageSource>(titleMetadataResult.Value.PosterTall)!;
        var posterWide = JsonSerializer.Deserialize<ImageSource>(titleMetadataResult.Value.PosterWide)!;

        return [
            new RemoteImageInfo
            {
                Url = posterTall.Uri,
                Width = posterTall.Width,
                Height = posterTall.Height,
                Type = ImageType.Primary
            },
            new RemoteImageInfo
            {
                Url = posterWide.Uri,
                Width = posterWide.Width,
                Height = posterWide.Height,
                Type = ImageType.Backdrop
            }
        ];
    }
}