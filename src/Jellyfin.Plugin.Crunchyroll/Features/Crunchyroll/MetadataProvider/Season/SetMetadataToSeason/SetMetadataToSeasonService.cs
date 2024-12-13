using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.SetMetadataToSeason;

public class SetMetadataToSeasonService : ISetMetadataToSeasonService
{
    private readonly ISetMetadataToSeasonRepository _repository;
    private readonly ILogger<SetMetadataToSeasonService> _logger;
    private readonly PluginConfiguration _config;

    public SetMetadataToSeasonService(ISetMetadataToSeasonRepository repository,
        ILogger<SetMetadataToSeasonService> logger, PluginConfiguration config)
    {
        _repository = repository;
        _logger = logger;
        _config = config;
    }
    
    public async Task<Result<MediaBrowser.Controller.Entities.TV.Season>> SetMetadataToSeasonAsync(CrunchyrollId seasonId, 
        CultureInfo language, int? currentIndexNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(seasonId?.ToString()))
        {
            return Result.Fail(ErrorCodes.NotAllowed);
        }
        
        var seasonResult = await _repository.GetSeasonAsync(seasonId, language, cancellationToken);

        if (seasonResult.IsFailed)
        {
            return seasonResult.ToResult();
        }

        var crunchyrollSeason = seasonResult.Value;

        if (crunchyrollSeason is null)
        {
            _logger.LogError("Season with crunchyrollId {SeasonId} not found", seasonId);
            return Result.Fail(ErrorCodes.NotFound);
        }

        var metadataSeason = new MediaBrowser.Controller.Entities.TV.Season();
        
        SetSeasonTitle(metadataSeason, crunchyrollSeason);
        SetSortNameToSequenceNumber(metadataSeason, crunchyrollSeason);
        
        //Set indexNumber otherwise jellyfin will recognize it as a specials season and will not show episodes with indexnumbers
        if (!currentIndexNumber.HasValue)
        {
            metadataSeason.IndexNumber = int.MaxValue - crunchyrollSeason.SeasonSequenceNumber;
        }
        else
        {
            metadataSeason.IndexNumber = currentIndexNumber;
        }

        return metadataSeason;
    }
    
    private void SetSortNameToSequenceNumber(MediaBrowser.Controller.Entities.TV.Season season, 
        Domain.Entities.Season crunchyrollSeason)
    {
        if (!_config.IsFeatureSeasonOrderByCrunchyrollOrderEnabled)
        {
            return;
        }
        
        season.ForcedSortName = crunchyrollSeason.SeasonSequenceNumber.ToString();
        season.PresentationUniqueKey = season.CreatePresentationUniqueKey(); //Create new key to visually split duplicate seasons
    }

    private void SetSeasonTitle(MediaBrowser.Controller.Entities.TV.Season season, Domain.Entities.Season crunchyrollSeason)
    {
        if (!_config.IsFeatureSeasonTitleEnabled)
        {
            return;
        }
        
        season.Name = !string.IsNullOrWhiteSpace(crunchyrollSeason.SeasonDisplayNumber) 
            ? $"S{crunchyrollSeason.SeasonDisplayNumber}: {crunchyrollSeason.Title}" 
            : crunchyrollSeason.Title;
    }
}