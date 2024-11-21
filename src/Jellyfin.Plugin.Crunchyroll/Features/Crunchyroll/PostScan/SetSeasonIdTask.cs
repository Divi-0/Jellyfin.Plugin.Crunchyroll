using FluentResults;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

public class SetSeasonIdTask : IPostTitleIdSetTask
{
    private readonly IMediator _mediator;
    private readonly IEnumerable<IPostSeasonIdSetTask> _postSeasonIdSetTasks;
    private readonly ILogger<SetSeasonIdTask> _logger;
    private readonly ILibraryManager _libraryManager;

    public SetSeasonIdTask(IMediator mediator, IEnumerable<IPostSeasonIdSetTask> postSeasonIdSetTasks,
        ILogger<SetSeasonIdTask> logger, ILibraryManager libraryManager)
    {
        _mediator = mediator;
        _postSeasonIdSetTasks = postSeasonIdSetTasks;
        _logger = logger;
        _libraryManager = libraryManager;
    }

    public async Task RunAsync(BaseItem seriesItem, CancellationToken cancellationToken)
    {
        seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out string? titleId);

        //Some seasons on crunchyroll like One Piece have duplicate SeasonNumbers
        var seasonNumberDuplicateCounters = new Dictionary<int, int>();
        foreach (var season in ((Folder)seriesItem).Children)
        {
            var setIdResult = await SetIdForSeason(seriesItem, titleId, season, seasonNumberDuplicateCounters, cancellationToken);

            if (setIdResult.IsFailed)
            {
                continue;
            }
            
            await RunPostTasks(season, cancellationToken);
        }
    }

    private async Task<Result> SetIdForSeason(BaseItem seriesItem, string? titleId, BaseItem season, 
        Dictionary<int, int> seasonNumberDuplicateCounters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(season.FileNameWithoutExtension))
        {
            _logger.LogDebug("Season {Name} has no folder name, Skipping...", season.Name);
            return Result.Fail(Domain.Constants.ErrorCodes.NotAllowed);
        }
        
        if (string.IsNullOrWhiteSpace(titleId))
        {
            _logger.LogDebug("TitleId for item with name {Name} is not set, skipping season id task...", seriesItem.FileNameWithoutExtension);
            return Result.Fail(Domain.Constants.ErrorCodes.ProviderIdNotSet);
        }
                
        var hasSeasonId = season.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeasonId, out string? seasonId) &&
                          !string.IsNullOrWhiteSpace(seasonId);

        if (hasSeasonId)
        {
            _logger.LogDebug("SeasonId for season with name {Name} is already set, skipping...", season.FileNameWithoutExtension);
            return Result.Ok();
        }

        try
        {
            //For example Attack on Titan has an "OAD" Season, then search by Folder Name
            var seasonIdResult = await GetSeasonIdByName(titleId, season, cancellationToken);

            if (seasonIdResult.IsFailed)
            {
                _logger.LogDebug("SeasonIdQuery failed. Skipping season with name {Name}", season.FileNameWithoutExtension);
                return seasonIdResult.ToResult();
            }

            if (seasonIdResult.Value is null)
            {
                if (!season.IndexNumber.HasValue)
                {
                    _logger.LogError("Item with name '{Name}' has no IndexNumber. Skipping...", season.FileNameWithoutExtension);
                    return Result.Fail(Domain.Constants.ErrorCodes.Internal);
                }
                
                seasonNumberDuplicateCounters.TryGetValue(season.IndexNumber.Value, out var seasonCounter);
                seasonIdResult = await GetSeasonIdByNumber(titleId, season, seasonCounter, cancellationToken);
                seasonCounter += 1;
                seasonNumberDuplicateCounters[season.IndexNumber.Value] = seasonCounter;
            }

            if (seasonIdResult.IsFailed)
            {
                _logger.LogDebug("SeasonIdQuery failed. Skipping season with name {Name}", season.FileNameWithoutExtension);
                return seasonIdResult.ToResult();
            }

            await UpdateSeasonItem(season, seasonIdResult.Value, cancellationToken);
            
            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown error on setting season id for {Name}", season.FileNameWithoutExtension);
            return Result.Fail(Domain.Constants.ErrorCodes.Internal);
        }
    }

    private async Task<Result<string?>> GetSeasonIdByName(string titleId, BaseItem season, CancellationToken cancellationToken)
    {
        var byNameQuery = new SeasonIdQueryByName(titleId, season.FileNameWithoutExtension);
        return await _mediator.Send(byNameQuery, cancellationToken);
    }

    private async Task<Result<string?>> GetSeasonIdByNumber(string titleId, BaseItem season, int seasonCounter, CancellationToken cancellationToken)
    {
        var query = new SeasonIdQueryByNumber(titleId, season.IndexNumber!.Value, seasonCounter);
        return await _mediator.Send(query, cancellationToken);
    }

    private async Task UpdateSeasonItem(BaseItem season, string? seasonId, CancellationToken cancellationToken)
    {
        season.ProviderIds[CrunchyrollExternalKeys.SeasonId] = seasonId ?? string.Empty;

        await _libraryManager.UpdateItemAsync(season, season.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);
    }

    private async Task RunPostTasks(BaseItem item, CancellationToken cancellationToken)
    {
        foreach (var task in _postSeasonIdSetTasks)
        {
            await task.RunAsync(item, cancellationToken);
        }
    }
}