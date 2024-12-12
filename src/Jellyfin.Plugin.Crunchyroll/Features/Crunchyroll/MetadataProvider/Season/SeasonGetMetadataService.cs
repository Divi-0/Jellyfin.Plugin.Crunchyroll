using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.GetSeasonCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.SetMetadataToSeason;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season;

public class SeasonGetMetadataService : ISeasonGetMetadataService
{
    private readonly ILogger<SeasonGetMetadataService> _logger;
    private readonly IGetSeasonCrunchyrollIdService _seasonCrunchyrollIdService;
    private readonly IScrapSeasonMetadataService _scrapSeasonMetadataService;
    private readonly ISetMetadataToSeasonService _setMetadataToSeasonService;

    public SeasonGetMetadataService(ILogger<SeasonGetMetadataService> logger,
        IGetSeasonCrunchyrollIdService seasonCrunchyrollIdService,
        IScrapSeasonMetadataService scrapSeasonMetadataService,
        ISetMetadataToSeasonService setMetadataToSeasonService)
    {
        _logger = logger;
        _seasonCrunchyrollIdService = seasonCrunchyrollIdService;
        _scrapSeasonMetadataService = scrapSeasonMetadataService;
        _setMetadataToSeasonService = setMetadataToSeasonService;
    }
    
    public async Task<MetadataResult<MediaBrowser.Controller.Entities.TV.Season>> GetMetadataAsync(SeasonInfo info, CancellationToken cancellationToken)
    {
        var folderName = Path.GetFileNameWithoutExtension(info.Path);
        var seriesId = info.SeriesProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeriesId);

        if (string.IsNullOrWhiteSpace(seriesId))
        {
            _logger.LogDebug("Series/Parent of season {Name} has no seriesId. Skipping...", folderName);
            return new MetadataResult<MediaBrowser.Controller.Entities.TV.Season>()
            {
                HasMetadata = false,
                Item = new MediaBrowser.Controller.Entities.TV.Season()
            };
        }
        
        var language = info.GetPreferredMetadataCultureInfo();

        //ignore failed result, because data can still exist in database
        _ = await _scrapSeasonMetadataService.ScrapSeasonMetadataAsync(seriesId, 
            language, cancellationToken);

        var seasonId = info.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeasonId);

        if (string.IsNullOrWhiteSpace(seasonId))
        {
            var getCrunchyrollIdResult = await _seasonCrunchyrollIdService.GetSeasonCrunchyrollId(seriesId, folderName, 
                info.IndexNumber, language, cancellationToken);

            if (getCrunchyrollIdResult.IsFailed)
            {
                return new MetadataResult<MediaBrowser.Controller.Entities.TV.Season>()
                {
                    HasMetadata = false,
                    Item = new MediaBrowser.Controller.Entities.TV.Season()
                };
            }

            if (getCrunchyrollIdResult.Value is null)
            {
                _logger.LogError("Crunchyroll season id for {Path} was not found", info.Path);
                return new MetadataResult<MediaBrowser.Controller.Entities.TV.Season>()
                {
                    HasMetadata = false,
                    Item = new MediaBrowser.Controller.Entities.TV.Season()
                };
            }
        
            seasonId = getCrunchyrollIdResult.Value;
        }

        var newSeasonMetadataResult = await _setMetadataToSeasonService.SetMetadataToSeasonAsync(seasonId, language, 
            info.IndexNumber, cancellationToken);

        if (newSeasonMetadataResult.IsFailed)
        {
            return new MetadataResult<MediaBrowser.Controller.Entities.TV.Season>()
            {
                HasMetadata = false,
                Item = new MediaBrowser.Controller.Entities.TV.Season()
            };
        }

        var seasonWithNewMetadata = newSeasonMetadataResult.Value;
        seasonWithNewMetadata.ProviderIds[CrunchyrollExternalKeys.SeasonId] = seasonId;

        return new MetadataResult<MediaBrowser.Controller.Entities.TV.Season>()
        {
            HasMetadata = true,
            Item = seasonWithNewMetadata
        };
    }
}