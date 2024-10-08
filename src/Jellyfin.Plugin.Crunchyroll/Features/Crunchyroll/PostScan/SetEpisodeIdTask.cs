using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

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
            var setEpisodeIdResult = await SetEpisodeId(episode, titleId, seasonId, cancellationToken);

            if (setEpisodeIdResult.IsFailed)
            {
                continue;
            }
            
            await RunPostTasks(episode, cancellationToken);
        }
    }

    private async ValueTask<Result> SetEpisodeId(BaseItem episode, string? titleId,
        string? seasonId, CancellationToken cancellationToken)
    {
        var hasEpisodeId = episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var existingEpisodeId) &&
                           !string.IsNullOrWhiteSpace(existingEpisodeId);
        
        var hasEpisodeSlugTitle = episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeSlugTitle, out var existingEpisodeSlugTitle) &&
                           !string.IsNullOrWhiteSpace(existingEpisodeSlugTitle);
        
        if (hasEpisodeId && hasEpisodeSlugTitle)
        {
            _logger.LogDebug("Episode with name {Name} has already a id and slugTitle. Skipping...", episode.Name);
            return Result.Ok();
        }
        
        if (!episode.IndexNumber.HasValue)
        {
            _logger.LogDebug("Episode {Name} has no Indexnumber. Skipping...", episode.Name);
            return Result.Fail(ErrorCodes.PreconditionFailed);
        }

        var episodeIdResult = await _mediator.Send(new EpisodeIdQuery(titleId!, seasonId!, episode.IndexNumber!.Value.ToString()), cancellationToken);

        if (episodeIdResult.IsFailed)
        {
            return episodeIdResult.ToResult();
        }
            
        var episodeId = episodeIdResult.Value?.EpisodeId ?? string.Empty;
        var episodeSlugTitle = episodeIdResult.Value?.EpisodeSlugTitle ?? string.Empty;
        episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId] = episodeId;
        episode.ProviderIds[CrunchyrollExternalKeys.EpisodeSlugTitle] = episodeSlugTitle;

        await _libraryManager.UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);
        
        return Result.Ok();
    }

    private async Task RunPostTasks(BaseItem episodeItem, CancellationToken cancellationToken)
    {
        foreach (var task in _postEpisodeIdSetTasks)
        {
            await task.RunAsync(episodeItem, cancellationToken);
        }
    }
}