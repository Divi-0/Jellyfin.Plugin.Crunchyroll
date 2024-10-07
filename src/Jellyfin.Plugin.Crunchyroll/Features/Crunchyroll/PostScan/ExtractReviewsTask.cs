using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;
using MediaBrowser.Controller.Entities;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

public class ExtractReviewsTask : IPostTitleIdSetTask
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExtractReviewsTask> _logger;
    private readonly PluginConfiguration _config;

    public ExtractReviewsTask(IMediator mediator, ILogger<ExtractReviewsTask> logger, PluginConfiguration config)
    {
        _mediator = mediator;
        _logger = logger;
        _config = config;
    }
    
    public async Task RunAsync(BaseItem seriesItem, CancellationToken cancellationToken)
    {
        if (!_config.IsWaybackMachineEnabled)
        {
            return;
        }
        
        var hasId = seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out string? id) &&
                    !string.IsNullOrWhiteSpace(id);

        var hasSlugTitle = seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SlugTitle, out string? slugTitle) &&
                           !string.IsNullOrWhiteSpace(slugTitle);

        if (!hasId || !hasSlugTitle)
        {
            //if item has no id or slugTitle, skip this item
            _logger.LogDebug("No crunchyroll ids found on series item {Name}. Skipping...", seriesItem.Name);
            return;
        }

        _ = await _mediator.Send(new ExtractReviewsCommand()
        {
            TitleId = id!,
            SlugTitle = slugTitle!
        }, cancellationToken);
    }
}