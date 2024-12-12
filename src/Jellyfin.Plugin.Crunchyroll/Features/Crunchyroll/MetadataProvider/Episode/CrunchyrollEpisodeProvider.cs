using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode;

public class CrunchyrollEpisodeProvider : IRemoteMetadataProvider<MediaBrowser.Controller.Entities.TV.Episode, EpisodeInfo>, IHasOrder
{
    private readonly ILogger<CrunchyrollEpisodeProvider> _logger;
    
    public string Name => "Crunchyroll";
    public int Order { get; } = 3;

    public CrunchyrollEpisodeProvider(ILogger<CrunchyrollEpisodeProvider> logger)
    {
        _logger = logger;
    }
    
    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public async Task<MetadataResult<MediaBrowser.Controller.Entities.TV.Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
    {
        try
        {
            var serviceScopeFactory =
                CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            using var scope = serviceScopeFactory.CreateScope();
            var getMetadataService = scope.ServiceProvider.GetRequiredService<IEpisodeGetMetadataService>();
            return await getMetadataService.GetMetadataAsync(info, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unexpected error");
            return new MetadataResult<MediaBrowser.Controller.Entities.TV.Episode>()
            {
                HasMetadata = false,
                Item = new MediaBrowser.Controller.Entities.TV.Episode()
            };
        }
    }
    
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClientFactory = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        return httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
    }
}