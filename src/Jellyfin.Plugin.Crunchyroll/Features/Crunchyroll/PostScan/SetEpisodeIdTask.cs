using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

public partial class SetEpisodeIdTask : IPostSeasonIdSetTask
{
    private readonly IMediator _mediator;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<SetEpisodeIdTask> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SetEpisodeIdTask(IMediator mediator, ILibraryManager libraryManager, ILogger<SetEpisodeIdTask> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _mediator = mediator;
        _libraryManager = libraryManager;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    public async Task RunAsync(BaseItem seasonItem, CancellationToken cancellationToken)
    {
        var hasSeasonId = seasonItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeasonId, out var seasonId) &&
                          !string.IsNullOrWhiteSpace(seasonId);

        if (!hasSeasonId)
        {
            //if the item has no indexNumber or already has a seasonId, ignore it
            _logger.LogDebug("Season {Name} has no Crunchyroll id. Skipping...", seasonItem.Name);
            return;
        }

        await Parallel.ForEachAsync(((Folder)seasonItem).Children, cancellationToken, async (episode, _) =>
        {
            var setEpisodeIdResult = await SetEpisodeId(episode, seasonId, cancellationToken);

            if (setEpisodeIdResult.IsFailed)
            {
                return;
            }

            await RunPostTasks(episode, cancellationToken);
        });
    }

    private async ValueTask<Result> SetEpisodeId(BaseItem episode,
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

        var episodeIdResult = await GetEpisodeIdAsync(episode, seasonId!, cancellationToken);

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

    private async Task<Result<EpisodeIdResult?>> GetEpisodeIdAsync(BaseItem episode, string seasonId, 
        CancellationToken cancellationToken)
    {
        string episodeIdentifier;

        if (IsDecimalNumberOrNumberWithLetterRegex().Match(episode.FileNameWithoutExtension).Success)
        {
            episode.IndexNumber = null;
        }
        
        if (!episode.IndexNumber.HasValue)
        {
            var match = EpisodeNameFormatRegex().Match(episode.FileNameWithoutExtension);

            if (!match.Success)
            {
                var episodeIdByNameResult = await _mediator.Send(new EpisodeIdQueryByName(seasonId, episode.FileNameWithoutExtension), cancellationToken);

                if (episodeIdByNameResult.IsSuccess)
                {
                    return episodeIdByNameResult.Value;
                }
                
                _logger.LogDebug("Episode {Name} has no IndexNumber, number could not be read from name and id was " +
                                 "not found by episode name. Skipping...", 
                    episode.Name);
                return Result.Fail(ErrorCodes.PreconditionFailed);
            }
            
            episodeIdentifier = match.Groups[1].Value.TrimStart('0');
        }
        else
        {
            episodeIdentifier = episode.IndexNumber.Value.ToString();
        }

        var episodeIdResult = await _mediator.Send(new EpisodeIdQuery(seasonId!, episodeIdentifier), cancellationToken);

        if (episodeIdResult.IsFailed)
        {
            return episodeIdResult.ToResult();
        }

        return episodeIdResult.Value;
    }

    private async Task RunPostTasks(BaseItem episodeItem, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var postEpisodeIdSetTasks = scope.ServiceProvider.GetServices<IPostEpisodeIdSetTask>();
        foreach (var task in postEpisodeIdSetTasks)
        {
            await task.RunAsync(episodeItem, cancellationToken);
        }
    }

    [GeneratedRegex(@"E\d*\.\d*|E\d*[A-z]")]
    private static partial Regex IsDecimalNumberOrNumberWithLetterRegex();

    [GeneratedRegex(@"E-?([^ -]+)")]
    private static partial Regex EpisodeNameFormatRegex();
}