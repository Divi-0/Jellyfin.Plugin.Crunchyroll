using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie;

public sealed class CrunchyrollMovieProvider : IRemoteMetadataProvider<MediaBrowser.Controller.Entities.Movies.Movie, MovieInfo>, IHasOrder
{
    private readonly ILogger<CrunchyrollMovieProvider> _logger;
    public string Name => "Crunchyroll";
    public int Order { get; } = 3;

    public CrunchyrollMovieProvider(ILogger<CrunchyrollMovieProvider> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Used to return search results to the user, so he can manually choose the providerId
    /// </summary>
    /// <param name="searchInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Gets the real metadata in a library scan
    /// </summary>
    /// <param name="info"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MetadataResult<MediaBrowser.Controller.Entities.Movies.Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
    {
        try
        {
            var serviceScopeFactory =
                CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            using var scope = serviceScopeFactory.CreateScope();
            var getMetadataService = scope.ServiceProvider.GetRequiredService<ICrunchyrollMovieGetMetadataService>();
            return await getMetadataService.GetMetadataAsync(info, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error");
            return new MetadataResult<MediaBrowser.Controller.Entities.Movies.Movie>()
            {
                HasMetadata = false,
                Item = new MediaBrowser.Controller.Entities.Movies.Movie()
            };
        }
    }
    
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClientFactory = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        return httpClientFactory.CreateClient(CrunchyrollHttpClientNames.ImageClient).GetAsync(url, cancellationToken);
    }
}