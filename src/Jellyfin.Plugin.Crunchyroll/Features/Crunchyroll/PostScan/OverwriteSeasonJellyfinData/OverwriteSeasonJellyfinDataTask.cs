using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeasonJellyfinData;

public sealed class OverwriteSeasonJellyfinDataTask : IPostSeasonIdSetTask
{
    private readonly ILogger<OverwriteSeasonJellyfinDataTask> _logger;
    private readonly IOverwriteSeasonJellyfinDataSession _session;
    private readonly ILibraryManager _libraryManager;
    private readonly PluginConfiguration _config;

    public OverwriteSeasonJellyfinDataTask(ILogger<OverwriteSeasonJellyfinDataTask> logger,
        IOverwriteSeasonJellyfinDataSession session, ILibraryManager libraryManager, PluginConfiguration config)
    {
        _logger = logger;
        _session = session;
        _libraryManager = libraryManager;
        _config = config;
    }
    
    public async Task RunAsync(BaseItem seasonItem, CancellationToken cancellationToken)
    {
        var hasSeasonId = seasonItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeasonId, out string? seasonId) &&
                           !string.IsNullOrWhiteSpace(seasonId);

        if (!hasSeasonId)
        {
            _logger.LogDebug("No SeasonId found for season with name {Name}. Skipping...", seasonItem.Name);
            return;
        }

        var seasonResult = await _session.GetSeasonAsync(seasonId!);

        if (seasonResult.IsFailed)
        {
            return;
        }

        var crunchyrollSeason = seasonResult.Value;

        seasonItem.Name = !string.IsNullOrWhiteSpace(crunchyrollSeason.SeasonDisplayNumber) 
            ? $"S{crunchyrollSeason.SeasonDisplayNumber}: {crunchyrollSeason.Title}" 
            : crunchyrollSeason.Title;
        
        await SetIndexNumberToSequenceNumber((Season)seasonItem, crunchyrollSeason, cancellationToken);

        try
        {
            await _libraryManager.UpdateItemAsync(seasonItem, seasonItem.DisplayParent, ItemUpdateType.MetadataEdit,
                cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to update Season with name {Name}", seasonItem.Name);
            return;
        }
    }

    private async Task SetIndexNumberToSequenceNumber(Season season, TitleMetadata.Entities.Season crunchyrollSeason,
        CancellationToken cancellationToken)
    {
        if (!_config.IsOrderSeasonsByCrunchyrollOrderEnabled)
        {
            return;
        }

        season.IndexNumber = crunchyrollSeason.SeasonSequenceNumber;
        season.PresentationUniqueKey = season.CreatePresentationUniqueKey(); //Create new key to visually split duplicate seasons

        foreach (var episode in season.Children)
        {
            episode.ParentIndexNumber = crunchyrollSeason.SeasonSequenceNumber;
            await _libraryManager.UpdateItemAsync(episode, season, ItemUpdateType.MetadataEdit, cancellationToken);
        }
    }
}