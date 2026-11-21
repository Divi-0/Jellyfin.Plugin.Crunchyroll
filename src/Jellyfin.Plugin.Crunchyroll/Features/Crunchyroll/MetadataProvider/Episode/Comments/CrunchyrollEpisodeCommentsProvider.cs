using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.Comments;

public class CrunchyrollEpisodeCommentsProvider : ICustomMetadataProvider<MediaBrowser.Controller.Entities.TV.Episode>
{
    private readonly ILogger<CrunchyrollEpisodeCommentsProvider> _logger;
    public string Name => "Crunchyroll-Comments";

    public CrunchyrollEpisodeCommentsProvider(ILogger<CrunchyrollEpisodeCommentsProvider> logger)
    {
        _logger = logger;
    }
    
    public Task<ItemUpdateType> FetchAsync(MediaBrowser.Controller.Entities.TV.Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var serviceProvider = CrunchyrollPlugin.Instance!.ServiceProvider;
            var config = serviceProvider.GetRequiredService<PluginConfiguration>();
            var queue = serviceProvider.GetRequiredService<WaybackBackgroundQueue>();
            queue.EnqueueIfEligible(
                config.IsFeatureCommentsEnabled,
                item.PremiereDate,
                (sp, ct) => sp.GetRequiredService<ICrunchyrollEpisodeCommentsService>().ScrapCommentsAsync(item, ct));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error");
        }

        return Task.FromResult(ItemUpdateType.None);
    }
}