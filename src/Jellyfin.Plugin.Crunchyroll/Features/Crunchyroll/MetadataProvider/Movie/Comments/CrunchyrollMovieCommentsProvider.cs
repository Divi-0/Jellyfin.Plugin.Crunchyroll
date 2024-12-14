using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Comments;

public class CrunchyrollMovieCommentsProvider : ICustomMetadataProvider<MediaBrowser.Controller.Entities.Movies.Movie>
{
    private readonly ILogger<CrunchyrollMovieCommentsProvider> _logger;
    public string Name => "Crunchyroll-Comments";

    public CrunchyrollMovieCommentsProvider(ILogger<CrunchyrollMovieCommentsProvider> logger)
    {
        _logger = logger;
    }
    
    public async Task<ItemUpdateType> FetchAsync(MediaBrowser.Controller.Entities.Movies.Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var serviceScopeFactory =
                CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            using var scope = serviceScopeFactory.CreateScope();
            var movieCommentsService = scope.ServiceProvider.GetRequiredService<ICrunchyrollMovieCommentsService>();
            return await movieCommentsService.ScrapCommentsAsync(item, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error");
            return ItemUpdateType.None;
        }
    }
}