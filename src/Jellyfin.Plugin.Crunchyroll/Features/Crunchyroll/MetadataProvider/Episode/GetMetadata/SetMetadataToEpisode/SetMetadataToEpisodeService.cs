using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.SetMetadataToEpisode;

public partial class SetMetadataToEpisodeService : ISetMetadataToEpisodeService
{
    private readonly ISetMetadataToEpisodeRepository _repository;
    private readonly ILogger<SetMetadataToEpisodeService> _logger;
    private readonly PluginConfiguration _config;

    public SetMetadataToEpisodeService(ISetMetadataToEpisodeRepository repository,
        ILogger<SetMetadataToEpisodeService> logger, PluginConfiguration config)
    {
        _repository = repository;
        _logger = logger;
        _config = config;
    }
    
    public async Task<Result<MediaBrowser.Controller.Entities.TV.Episode>> SetMetadataToEpisodeAsync(CrunchyrollId episodeId, 
        int? currentIndexNumber, int? parentIndexNumber, CultureInfo language, CancellationToken cancellationToken)
    {
        var crunchyrollEpisodeResult = await _repository.GetEpisodeAsync(episodeId, 
            language, cancellationToken);

        if (crunchyrollEpisodeResult.IsFailed)
        {
            return crunchyrollEpisodeResult.ToResult();
        }
        
        var crunchyrollEpisode = crunchyrollEpisodeResult.Value;

        if (crunchyrollEpisode is null)
        {
            _logger.LogError("episode with crunchyroll episodeId {EpisodeId} was not found", episodeId);
            return Result.Fail(ErrorCodes.NotFound);
        }

        var episodeWithNewMetadata = new MediaBrowser.Controller.Entities.TV.Episode();
        SetEpisodeTitle(episodeWithNewMetadata, crunchyrollEpisode);
        SetEpisodeOverview(episodeWithNewMetadata, crunchyrollEpisode);
        
        if (!currentIndexNumber.HasValue)
        {
            SetIndexNumberAndName(episodeWithNewMetadata, crunchyrollEpisode, parentIndexNumber);
        }

        return episodeWithNewMetadata;
    }
    
    private void SetIndexNumberAndName(MediaBrowser.Controller.Entities.TV.Episode episode, 
        Domain.Entities.Episode crunchyrollEpisode, int? parentIndexNumber)
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
            SetSpecialEpisodeAirsBefore(episode, crunchyrollEpisode, parentIndexNumber);
        }
            
        episode.Name = $"{crunchyrollEpisode.EpisodeNumber} - {crunchyrollEpisode.Title}";
    }

    private static void SetSpecialEpisodeAirsBefore(MediaBrowser.Controller.Entities.TV.Episode episode, 
        Domain.Entities.Episode crunchyrollEpisode, int? parentIndexNumber)
    {
        //Add 0.5 to sequenceNumber, because every special episode between normal episodes are decimals with x.5
        episode.AirsBeforeEpisodeNumber = Convert.ToInt32(crunchyrollEpisode.SequenceNumber + 0.5);
        episode.AirsBeforeSeasonNumber = crunchyrollEpisode.Season!.SeasonNumber;
    }

    private void SetEpisodeTitle(MediaBrowser.Controller.Entities.TV.Episode episode, Domain.Entities.Episode crunchyrollEpisode)
    {
        if (!_config.IsFeatureEpisodeTitleEnabled)
        {
            return;
        }
        
        var match = NameWithBracketsRegex().Match(crunchyrollEpisode.Title);
        episode.Name = match.Success 
            ? match.Groups[1].Value 
            : crunchyrollEpisode.Title;
    }

    private void SetEpisodeOverview(MediaBrowser.Controller.Entities.TV.Episode episode, Domain.Entities.Episode crunchyrollEpisode)
    {
        if (!_config.IsFeatureEpisodeDescriptionEnabled)
        {
            return;
        }
        
        episode.Overview = crunchyrollEpisode.Description;
    }
    
    [GeneratedRegex(@"\d+")]
    private static partial Regex EpisodeNumberRegex();
    [GeneratedRegex(@"\(.*\) (.*)")]
    private static partial Regex NameWithBracketsRegex();
}