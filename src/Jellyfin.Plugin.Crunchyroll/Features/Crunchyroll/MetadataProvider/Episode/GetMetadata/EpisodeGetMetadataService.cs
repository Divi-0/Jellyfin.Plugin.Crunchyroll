using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetEpisodeCrunchyrollId;
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

    public EpisodeGetMetadataService(ILogger<EpisodeGetMetadataService> logger,
        IGetEpisodeCrunchyrollIdService episodeCrunchyrollIdService,
        IScrapEpisodeMetadataService scrapEpisodeMetadataService,
        ISetMetadataToEpisodeService setMetadataToEpisodeService)
    {
        _logger = logger;
        _episodeCrunchyrollIdService = episodeCrunchyrollIdService;
        _scrapEpisodeMetadataService = scrapEpisodeMetadataService;
        _setMetadataToEpisodeService = setMetadataToEpisodeService;
    }
    
    public async Task<MetadataResult<MediaBrowser.Controller.Entities.TV.Episode>> GetMetadataAsync(EpisodeInfo info, CancellationToken cancellationToken)
    {
        var seasonId = info.SeasonProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeasonId);

        if (string.IsNullOrWhiteSpace(seasonId))
        {
            _logger.LogDebug("Parent of episode {Path} has no Crunchyroll season id. Skipping...", info.Path);
            return new MetadataResult<MediaBrowser.Controller.Entities.TV.Episode>()
            {
                HasMetadata = false,
                Item = new MediaBrowser.Controller.Entities.TV.Episode()
            };
        }

        var episodeId = info.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.EpisodeId);

        //ignore result
        _ = await _scrapEpisodeMetadataService.ScrapEpisodeMetadataAsync(seasonId,
            info.GetPreferredMetadataCultureInfo(), cancellationToken);

        if (string.IsNullOrWhiteSpace(episodeId))
        {
            var episodeIdResult = await _episodeCrunchyrollIdService.GetEpisodeIdAsync(seasonId,
                Path.GetFileNameWithoutExtension(info.Path), info.IndexNumber, cancellationToken);

            if (episodeIdResult.IsFailed)
            {
                return new MetadataResult<MediaBrowser.Controller.Entities.TV.Episode>()
                {
                    HasMetadata = false,
                    Item = new MediaBrowser.Controller.Entities.TV.Episode()
                };
            }

            if (episodeIdResult.Value is null)
            {
                _logger.LogDebug("Could not find any episodeId for episode {Path}", info.Path);
                return new MetadataResult<MediaBrowser.Controller.Entities.TV.Episode>()
                {
                    HasMetadata = false,
                    Item = new MediaBrowser.Controller.Entities.TV.Episode()
                };
            }

            episodeId = episodeIdResult.Value;
        }

        var episodeWithNewMetadataResult = await _setMetadataToEpisodeService
            .SetMetadataToEpisodeAsync(episodeId, info.IndexNumber, info.ParentIndexNumber,
                info.GetPreferredMetadataCultureInfo(), cancellationToken);

        if (episodeWithNewMetadataResult.IsFailed)
        {
            return new MetadataResult<MediaBrowser.Controller.Entities.TV.Episode>()
            {
                HasMetadata = false,
                Item = new MediaBrowser.Controller.Entities.TV.Episode()
            };
        }

        var newEpisode = episodeWithNewMetadataResult.Value;
        newEpisode.ProviderIds[CrunchyrollExternalKeys.EpisodeId] = episodeId;
        
        return new MetadataResult<MediaBrowser.Controller.Entities.TV.Episode>()
        {
            HasMetadata = true,
            Item = episodeWithNewMetadataResult.Value
        };
    }
}