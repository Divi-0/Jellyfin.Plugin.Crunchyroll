using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode.GetEpisodeImageInfos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series.GetSeriesImageInfos;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode;

public class CrunchyrollEpisodeImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly ILogger<CrunchyrollEpisodeImageProvider> _logger;
    public string Name => "Crunchyroll";
    public int Order { get; } = 3;

    public CrunchyrollEpisodeImageProvider(ILogger<CrunchyrollEpisodeImageProvider> logger)
    {
        _logger = logger;
    }
    
    public bool Supports(BaseItem item)
    {
        return item is MediaBrowser.Controller.Entities.TV.Episode;
    }

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) =>
    [
        ImageType.Primary,
        ImageType.Thumb
    ];

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        try
        {
            var serviceScopeFactory =
                CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            using var scope = serviceScopeFactory.CreateScope();
            var getEpisodeImageInfosService = scope.ServiceProvider.GetRequiredService<IGetEpisodeImageInfosService>();
            return await getEpisodeImageInfosService.GetImageInfosAsync((MediaBrowser.Controller.Entities.TV.Episode)item, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error");
            return [];
        }
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClientFactory = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        return httpClientFactory.CreateClient(CrunchyrollHttpClientNames.ImageClient).GetAsync(url, cancellationToken);
    }
}