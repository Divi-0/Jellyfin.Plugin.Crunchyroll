using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.Reviews;

public class CrunchyrollSeriesReviewsProvider : ICustomMetadataProvider<MediaBrowser.Controller.Entities.TV.Series>
{
    private readonly ILogger<CrunchyrollSeriesReviewsProvider> _logger;
    public string Name => "Crunchyroll-Reviews";

    public CrunchyrollSeriesReviewsProvider(ILogger<CrunchyrollSeriesReviewsProvider> logger)
    {
        _logger = logger;
    }
    
    public Task<ItemUpdateType> FetchAsync(MediaBrowser.Controller.Entities.TV.Series item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var serviceProvider = CrunchyrollPlugin.Instance!.ServiceProvider;
            var config = serviceProvider.GetRequiredService<PluginConfiguration>();
            var queue = serviceProvider.GetRequiredService<WaybackBackgroundQueue>();
            queue.EnqueueIfEligible(
                config.IsFeatureReviewsEnabled,
                item.PremiereDate,
                (sp, ct) => sp.GetRequiredService<ICrunchyrollSeriesReviewsService>().ScrapReviewsAsync(item, ct));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error");
        }

        return Task.FromResult(ItemUpdateType.None);
    }
}