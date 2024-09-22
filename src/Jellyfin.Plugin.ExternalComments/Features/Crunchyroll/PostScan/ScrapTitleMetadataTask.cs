using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using MediaBrowser.Controller.Entities;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;

public class ScrapTitleMetadataTask : IPostTitleIdSetTask
{
    private readonly IMediator _mediator;
    private readonly ILogger<ScrapTitleMetadataTask> _logger;

    public ScrapTitleMetadataTask(IMediator mediator, ILogger<ScrapTitleMetadataTask> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    public async Task RunAsync(BaseItem seriesItem, CancellationToken cancellationToken)
    {
        var hasId = seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out var id) &&
                    !string.IsNullOrWhiteSpace(id);

        var hasSlugTitle = seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SlugTitle, out var slugTitle) &&
                           !string.IsNullOrWhiteSpace(slugTitle);

        if (!hasId || !hasSlugTitle)
        {
            //if item has no id or slugTitle, skip this item
            _logger.LogDebug("No crunchyroll ids found on series item {Name}. Skipping...", seriesItem.Name);
            return;
        }

        _ = await _mediator.Send(new ScrapTitleMetadataCommand()
        {
            TitleId = id!,
            SlugTitle = slugTitle!
        }, cancellationToken);
    }
}