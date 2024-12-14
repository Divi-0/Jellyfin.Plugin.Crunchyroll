using System;
using System.Threading;
using System.Threading.Tasks;
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
    
    public async Task<ItemUpdateType> FetchAsync(MediaBrowser.Controller.Entities.TV.Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var serviceScopeFactory =
                CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            using var scope = serviceScopeFactory.CreateScope();
            var getMetadataService = scope.ServiceProvider.GetRequiredService<ICrunchyrollEpisodeCommentsService>();
            return await getMetadataService.ScrapCommentsAsync(item, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error");
            return ItemUpdateType.None;
        }
    }
}