using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.ExtractComments;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using MediaBrowser.Controller.Entities;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;

public sealed class ExtractCommentsTask : IPostEpisodeIdSetTask
{
    private readonly IMediator _mediator;
    private readonly PluginConfiguration _config;
    private readonly ILogger<ExtractCommentsTask> _logger;

    public ExtractCommentsTask(IMediator mediator, PluginConfiguration config, ILogger<ExtractCommentsTask> logger)
    {
        _mediator = mediator;
        _config = config;
        _logger = logger;
    }
    
    public async Task RunAsync(BaseItem episodeItem, CancellationToken cancellationToken)
    {
        if (!_config.IsWaybackMachineEnabled)
        {
            return;
        }
        
        var hasId = episodeItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out string? id) &&
                    !string.IsNullOrWhiteSpace(id);

        var hasSlugTitle = episodeItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeSlugTitle, out string? slugTitle) &&
                           !string.IsNullOrWhiteSpace(slugTitle);

        if (!hasId || !hasSlugTitle)
        {
            //if item has no id or slugTitle, skip this item
            _logger.LogDebug("No crunchyroll ids found on episode item {Name}. Skipping...", episodeItem.Name);
            return;
        }

        _ = await _mediator.Send(new ExtractCommentsCommand(id!, slugTitle!), cancellationToken);
    }
}