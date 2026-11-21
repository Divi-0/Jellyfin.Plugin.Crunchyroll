using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
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
    
    public Task<ItemUpdateType> FetchAsync(MediaBrowser.Controller.Entities.Movies.Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var serviceProvider = CrunchyrollPlugin.Instance!.ServiceProvider;
            var config = serviceProvider.GetRequiredService<PluginConfiguration>();
            var queue = serviceProvider.GetRequiredService<WaybackBackgroundQueue>();
            queue.EnqueueIfEligible(
                config.IsFeatureCommentsEnabled,
                item.PremiereDate,
                (sp, ct) => sp.GetRequiredService<ICrunchyrollMovieCommentsService>().ScrapCommentsAsync(item, ct));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error");
        }

        return Task.FromResult(ItemUpdateType.None);
    }
}