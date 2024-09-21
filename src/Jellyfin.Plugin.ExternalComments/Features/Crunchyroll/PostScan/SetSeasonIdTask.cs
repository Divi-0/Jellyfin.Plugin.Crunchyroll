using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan
{
    public class SetSeasonIdTask : IPostTitleIdSetTask
    {
        private readonly IMediator _mediator;
        private readonly IPostSeasonIdSetTask[] _postSeasonIdSetTasks;
        private readonly ILogger<SetSeasonIdTask> _logger;
        private readonly ILibraryManager _libraryManager;

        public SetSeasonIdTask(IMediator mediator, IPostSeasonIdSetTask[] postSeasonIdSetTasks,
            ILogger<SetSeasonIdTask> logger, ILibraryManager libraryManager)
        {
            _mediator = mediator;
            _postSeasonIdSetTasks = postSeasonIdSetTasks;
            _logger = logger;
            _libraryManager = libraryManager;
        }

        public async Task RunAsync(BaseItem seriesItem, CancellationToken cancellationToken)
        {
            seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out string? titleId);

            //Some seasons on crunchyroll like One Piece have duplicate SeasonNumbers
            var seasonNumberDuplicateCounters = new Dictionary<int, int>();
            foreach (var season in ((Folder)seriesItem).Children)
            {
                await SetIdForSeason(seriesItem, titleId, season, seasonNumberDuplicateCounters, cancellationToken);
                await RunPostTasks(season, cancellationToken);
            }
        }

        private async Task SetIdForSeason(BaseItem seriesItem, string? titleId, BaseItem season, 
            Dictionary<int, int> seasonNumberDuplicateCounters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(titleId))
            {
                _logger.LogDebug("TitleId for item with name {Name} is not set, skipping season id task...", seriesItem.Name);
                return;
            }
                
            var hasSeasonId = season.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeasonId, out string? seasonId) &&
                              !string.IsNullOrWhiteSpace(seasonId);

            if (!season.IndexNumber.HasValue || hasSeasonId)
            {
                _logger.LogDebug("SeasonId for season with name {Name} is already set, skipping...", season.Name);
                return;
            }

            try
            {
                seasonNumberDuplicateCounters.TryGetValue(season.IndexNumber.Value, out var seasonCounter);

                //For example Attack on Titan has an "OAD" Season, then search by Folder Name
                var seasonIdResult = await GetSeasonIdByName(titleId, season, cancellationToken);

                if (seasonIdResult.IsFailed)
                {
                    _logger.LogDebug("SeasonIdQuery failed. Skipping season with name {Name}", season.Name);
                    return;
                }

                if (seasonIdResult.Value is null)
                {
                    seasonIdResult = await GetSeasonIdByNumber(titleId, season, seasonCounter, cancellationToken);
                    seasonCounter += 1;
                    seasonNumberDuplicateCounters[season.IndexNumber.Value] = seasonCounter;
                }

                if (seasonIdResult.IsFailed)
                {
                    _logger.LogDebug("SeasonIdQuery failed. Skipping season with name {Name}", season.Name);
                    return;
                }

                await UpdateSeasonItem(season, seasonIdResult.Value, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unknown error on setting season id for {Name}", season.Name);
            }
        }

        private async Task<Result<string?>> GetSeasonIdByName(string titleId, BaseItem season, CancellationToken cancellationToken)
        {
            var byNameQuery = new SeasonIdQueryByName(titleId, season.Name);
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
}
