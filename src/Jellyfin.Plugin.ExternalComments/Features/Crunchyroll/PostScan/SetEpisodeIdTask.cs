using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetEpisodeId;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;

public class SetEpisodeIdTask : IPostSeasonIdSetTask
{
    private readonly IMediator _mediator;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<SetEpisodeIdTask> _logger;
    private readonly IEnumerable<IPostEpisodeIdSetTask> _postEpisodeIdSetTasks;

    public SetEpisodeIdTask(IMediator mediator, ILibraryManager libraryManager, ILogger<SetEpisodeIdTask> logger,
        IEnumerable<IPostEpisodeIdSetTask> postEpisodeIdSetTasks)
    {
        _mediator = mediator;
        _libraryManager = libraryManager;
        _logger = logger;
        _postEpisodeIdSetTasks = postEpisodeIdSetTasks;
    }
    
    public async Task RunAsync(BaseItem seasonItem, CancellationToken cancellationToken)
    {
        var hasTitleId = seasonItem.DisplayParent.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out var titleId) &&
                          !string.IsNullOrWhiteSpace(titleId);
        
        var hasSeasonId = seasonItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeasonId, out var seasonId) &&
                          !string.IsNullOrWhiteSpace(seasonId);

        if (!hasTitleId || !hasSeasonId)
        {
            //if the item has no indexNumber or already has a seasonId, ignore it
            _logger.LogDebug("Season {Name} has no Crunchyroll id. Skipping...", seasonItem.Name);
            return;
        }

        foreach (var episode in ((Folder)seasonItem).Children)
        {
            if (!episode.IndexNumber.HasValue)
            {
                _logger.LogDebug("Episode {Name} has no Indexnumber. Skipping...", episode.Name);
                continue;
            }

            var episodeIdResult = await _mediator.Send(new EpisodeIdQuery(titleId!, seasonId!, episode.IndexNumber!.Value.ToString()), cancellationToken);

            if (episodeIdResult.IsFailed)
            {
                continue;
            }
            
            episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId] = episodeIdResult.Value ?? string.Empty;

            await _libraryManager.UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);

            await RunPostTasks(episode, cancellationToken);
        }
    }

    private async Task RunPostTasks(BaseItem episodeItem, CancellationToken cancellationToken)
    {
        foreach (var task in _postEpisodeIdSetTasks)
        {
            await task.RunAsync(episodeItem, cancellationToken);
        }
    }
}