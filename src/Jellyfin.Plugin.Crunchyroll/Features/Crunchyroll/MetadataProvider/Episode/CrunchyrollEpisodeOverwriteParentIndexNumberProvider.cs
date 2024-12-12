using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.EpisodeOverwriteParentIndexNumber;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode;

public class CrunchyrollEpisodeOverwriteParentIndexNumberProvider : ICustomMetadataProvider<MediaBrowser.Controller.Entities.TV.Episode>
{
    private readonly ILogger<CrunchyrollEpisodeOverwriteParentIndexNumberProvider> _logger;

    public CrunchyrollEpisodeOverwriteParentIndexNumberProvider(
        ILogger<CrunchyrollEpisodeOverwriteParentIndexNumberProvider> logger)
    {
        _logger = logger;
    }
    
    public Task<ItemUpdateType> FetchAsync(MediaBrowser.Controller.Entities.TV.Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var overwriteParentIndexNumberService = CrunchyrollPlugin.Instance!.ServiceProvider
                .GetRequiredService<IEpisodeOverwriteParentIndexNumberService>();

            return overwriteParentIndexNumberService.SetParentIndexAsync(item);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error");
            return Task.FromResult(ItemUpdateType.None);
        }
    }

    public string Name => "Crunchyroll (Special Episodes in Season folder)";
}