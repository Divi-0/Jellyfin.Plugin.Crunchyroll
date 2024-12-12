using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series.GetSeriesImageInfos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series;

public sealed class CrunchyrollSeriesImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly ILogger<CrunchyrollSeriesImageProvider> _logger;
    public string Name => "Crunchyroll";
    public int Order { get; } = 3;

    public CrunchyrollSeriesImageProvider(ILogger<CrunchyrollSeriesImageProvider> logger)
    {
        _logger = logger;
    }
    
    public bool Supports(BaseItem item)
    {
        return item is MediaBrowser.Controller.Entities.TV.Series;
    }

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) =>
    [
        ImageType.Primary,
        ImageType.Backdrop
    ];

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        try
        {
            var serviceScopeFactory =
                CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            using var scope = serviceScopeFactory.CreateScope();
            var getSeriesImageInfosService = scope.ServiceProvider.GetRequiredService<IGetSeriesImageInfosService>();
            return await getSeriesImageInfosService.GetImageInfosAsync((MediaBrowser.Controller.Entities.TV.Series)item, cancellationToken);
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