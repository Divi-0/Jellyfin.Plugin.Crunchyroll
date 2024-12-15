using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetEpisodeCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetSpecialEpisodeCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.SetMetadataToEpisode;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata;

public class EpisodeGetMetadataService : IEpisodeGetMetadataService
{
    private readonly ILogger<EpisodeGetMetadataService> _logger;
    private readonly IGetEpisodeCrunchyrollIdService _episodeCrunchyrollIdService;
    private readonly IScrapEpisodeMetadataService _scrapEpisodeMetadataService;
    private readonly ISetMetadataToEpisodeService _setMetadataToEpisodeService;
    private readonly IGetSpecialEpisodeCrunchyrollIdService _specialEpisodeCrunchyrollIdService;

    public EpisodeGetMetadataService(ILogger<EpisodeGetMetadataService> logger,
        IGetEpisodeCrunchyrollIdService episodeCrunchyrollIdService,
        IScrapEpisodeMetadataService scrapEpisodeMetadataService,
        ISetMetadataToEpisodeService setMetadataToEpisodeService,
        IGetSpecialEpisodeCrunchyrollIdService specialEpisodeCrunchyrollIdService)
    {
        _logger = logger;
        _episodeCrunchyrollIdService = episodeCrunchyrollIdService;
        _scrapEpisodeMetadataService = scrapEpisodeMetadataService;
        _setMetadataToEpisodeService = setMetadataToEpisodeService;
        _specialEpisodeCrunchyrollIdService = specialEpisodeCrunchyrollIdService;
    }
    
    public async Task<MetadataResult<MediaBrowser.Controller.Entities.TV.Episode>> GetMetadataAsync(EpisodeInfo info, CancellationToken cancellationToken)
    {
        var seasonId = info.SeasonProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeasonId);
        var language = info.GetPreferredMetadataCultureInfo();
        
        if (!IsEpisodeInsideOfSpecialsSeason(info))
        {
            if (string.IsNullOrWhiteSpace(seasonId))
            {
                _logger.LogDebug("Parent of episode {Path} has no Crunchyroll season id. Skipping...", info.Path);
                return FailedResult;
            }

            //ignore result
            _ = await _scrapEpisodeMetadataService.ScrapEpisodeMetadataAsync(seasonId,
                language, cancellationToken);
        }
        
        var episodeId = info.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.EpisodeId);

        if (string.IsNullOrWhiteSpace(episodeId))
        {
            var fileName = Path.GetFileNameWithoutExtension(info.Path);
            var seriesId = info.SeriesProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeriesId);
            
            if (string.IsNullOrWhiteSpace(seriesId))
            {
                _logger.LogDebug("special episode {Path} has no seriesId, skipping...", info.Path);
                return FailedResult;
            }
            
            Result<CrunchyrollId?> episodeIdResult;
            if (IsEpisodeInsideOfSpecialsSeason(info))
            {
                episodeIdResult = await _specialEpisodeCrunchyrollIdService
                    .GetEpisodeIdAsync(seriesId, fileName, cancellationToken);
            }
            else
            {
                episodeIdResult = await _episodeCrunchyrollIdService.GetEpisodeIdAsync(seasonId!,
                    seriesId, language, fileName, info.IndexNumber, cancellationToken);
            }

            if (episodeIdResult.IsFailed)
            {
                return FailedResult;
            }

            if (episodeIdResult.Value is null)
            {
                _logger.LogDebug("Could not find any episodeId for episode {Path}", info.Path);
                return FailedResult;
            }

            episodeId = episodeIdResult.Value;
        }

        var episodeWithNewMetadataResult = await _setMetadataToEpisodeService
            .SetMetadataToEpisodeAsync(episodeId, info.IndexNumber, info.ParentIndexNumber,
                language, cancellationToken);

        if (episodeWithNewMetadataResult.IsFailed)
        {
            return FailedResult;
        }

        var newEpisode = episodeWithNewMetadataResult.Value;
        newEpisode.ProviderIds[CrunchyrollExternalKeys.EpisodeId] = episodeId;
        
        return new MetadataResult<MediaBrowser.Controller.Entities.TV.Episode>()
        {
            HasMetadata = true,
            Item = episodeWithNewMetadataResult.Value
        };
    }
    
    private MetadataResult<MediaBrowser.Controller.Entities.TV.Episode> FailedResult
        => new()
        {
            HasMetadata = false,
            Item = new MediaBrowser.Controller.Entities.TV.Episode()
        };

    private static bool IsEpisodeInsideOfSpecialsSeason(EpisodeInfo info)
    {
        return info.ParentIndexNumber == 0;
    }
}