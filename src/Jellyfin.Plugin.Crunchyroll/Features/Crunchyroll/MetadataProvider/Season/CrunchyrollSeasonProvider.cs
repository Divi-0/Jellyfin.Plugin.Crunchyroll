using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season;

public sealed class CrunchyrollSeasonProvider : IRemoteMetadataProvider<MediaBrowser.Controller.Entities.TV.Season, SeasonInfo>, IHasOrder
{
    private readonly ILogger<CrunchyrollSeasonProvider> _logger;
    public string Name => "Crunchyroll";
    public int Order { get; } = 3;

    public CrunchyrollSeasonProvider(ILogger<CrunchyrollSeasonProvider> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Used to return search results to the user, so he can manually choose the providerId
    /// </summary>
    /// <param name="searchInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo searchInfo, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Gets the real metadata in a library scan
    /// </summary>
    /// <param name="info"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MetadataResult<MediaBrowser.Controller.Entities.TV.Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
    {
        try
        {
            var serviceScopeFactory =
                CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            using var scope = serviceScopeFactory.CreateScope();
            var getMetadataService = scope.ServiceProvider.GetRequiredService<ISeasonGetMetadataService>();
            return await getMetadataService.GetMetadataAsync(info, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unexpected error");
            return new MetadataResult<MediaBrowser.Controller.Entities.TV.Season>()
            {
                HasMetadata = false,
                Item = new MediaBrowser.Controller.Entities.TV.Season()
            };
        }
    }
    
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}