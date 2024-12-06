using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;

public partial class OverwriteEpisodeJellyfinDataTask : IPostEpisodeIdSetTask
{
    private readonly ILogger<OverwriteEpisodeJellyfinDataTask> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IOverwriteEpisodeJellyfinDataTaskRepository _repository;
    private readonly ISetEpisodeThumbnail _setEpisodeThumbnail;
    private readonly PluginConfiguration _config;

    public OverwriteEpisodeJellyfinDataTask(ILogger<OverwriteEpisodeJellyfinDataTask> logger,
        ILibraryManager libraryManager, IOverwriteEpisodeJellyfinDataTaskRepository repository,
        ISetEpisodeThumbnail setEpisodeThumbnail, PluginConfiguration config)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _repository = repository;
        _setEpisodeThumbnail = setEpisodeThumbnail;
        _config = config;
    }
    
    public async Task RunAsync(BaseItem episodeItem, CancellationToken cancellationToken)
    {
        var hasEpisodeId = episodeItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, 
                               out var episodeId) && !string.IsNullOrWhiteSpace(episodeId);

        if (!hasEpisodeId)
        {
            _logger.LogDebug("Episode with name {Name} has no crunchyroll id. Skipping...", episodeItem.Name);
            return;
        }

        var crunchyrollEpisodeResult = await _repository.GetEpisodeAsync(episodeId!, 
            episodeItem.GetPreferredMetadataCultureInfo(), cancellationToken);

        if (crunchyrollEpisodeResult.IsFailed)
        {
            return;
        }
        
        var crunchyrollEpisode = crunchyrollEpisodeResult.Value;

        if (crunchyrollEpisode is null)
        {
            _logger.LogError("episode with crunchyroll episodeId {EpisodeId} was not found", episodeId);
            return;
        }

        await SetEpisodeThumbnailAsync(episodeItem, crunchyrollEpisode, cancellationToken);
        
        SetEpisodeTitle(episodeItem, crunchyrollEpisode);
        SetEpisodeOverview(episodeItem, crunchyrollEpisode);

        if (!episodeItem.IndexNumber.HasValue)
        {
            SetIndexNumberAndName((Episode)episodeItem, crunchyrollEpisode);
        }
        
        await _libraryManager.UpdateItemAsync(episodeItem, episodeItem.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);
    }

    private void SetIndexNumberAndName(Episode episode, TitleMetadata.Entities.Episode crunchyrollEpisode)
    {
        if (!_config.IsFeatureEpisodeIncludeSpecialsInNormalSeasonsEnabled)
        {
            return;
        }
        
        if (string.IsNullOrWhiteSpace(crunchyrollEpisode.EpisodeNumber))
        {
            return;
        }
            
        var match = EpisodeNumberRegex().Match(crunchyrollEpisode.EpisodeNumber);

        //if regex matches and episodeNumber is not sequence number then the episode is part of a normal season
        //e.g. One Piece Season 13 has special episodes in between 
        if (match.Success && Math.Abs(double.Parse(match.Value) - crunchyrollEpisode.SequenceNumber) < 0.5)
        {
            episode.IndexNumber = int.Parse(match.Value);
        }
        else
        {
            SetSpecialEpisodeAirsBefore(episode, crunchyrollEpisode.SequenceNumber);
        }
            
        episode.Name = $"{crunchyrollEpisode.EpisodeNumber} - {crunchyrollEpisode.Title}";
            
        if (crunchyrollEpisode.SequenceNumber % 1 != 0)
        {
            episode.ProviderIds[CrunchyrollExternalKeys.EpisodeDecimalEpisodeNumber] = 
                crunchyrollEpisode.SequenceNumber.ToString("0.0");
        }
    }

    private static void SetSpecialEpisodeAirsBefore(Episode episode, double crunchyrollSequenceNumber)
    {
        //Add 0.5 to sequenceNumber, because every special episode between normal episodes are decimals with x.5
        episode.AirsBeforeEpisodeNumber = Convert.ToInt32(crunchyrollSequenceNumber + 0.5);
        episode.AirsBeforeSeasonNumber = episode.Season.IndexNumber;
        episode.ParentIndexNumber = 0; //Manipulate ParentIndex to Season 0 so that Jellyfin thinks it is a special
    }

    private void SetEpisodeTitle(BaseItem item, TitleMetadata.Entities.Episode crunchyrollEpisode)
    {
        if (!_config.IsFeatureEpisodeTitleEnabled)
        {
            return;
        }
        
        var match = NameWithBracketsRegex().Match(crunchyrollEpisode.Title);
        item.Name = match.Success 
            ? match.Groups[1].Value 
            : crunchyrollEpisode.Title;
    }

    private void SetEpisodeOverview(BaseItem item, TitleMetadata.Entities.Episode crunchyrollEpisode)
    {
        if (!_config.IsFeatureEpisodeDescriptionEnabled)
        {
            return;
        }
        
        item.Overview = crunchyrollEpisode.Description;
    }

    private async Task SetEpisodeThumbnailAsync(BaseItem item, TitleMetadata.Entities.Episode crunchyrollEpisode,
        CancellationToken cancellationToken)
    {
        if (!_config.IsFeatureEpisodeThumbnailImageEnabled)
        {
            return;
        }
        
        var thumbnail = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
        _ = await _setEpisodeThumbnail
            .GetAndSetThumbnailAsync((Episode)item, thumbnail, cancellationToken);
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex EpisodeNumberRegex();
    [GeneratedRegex(@"\(.*\) (.*)")]
    private static partial Regex NameWithBracketsRegex();
}