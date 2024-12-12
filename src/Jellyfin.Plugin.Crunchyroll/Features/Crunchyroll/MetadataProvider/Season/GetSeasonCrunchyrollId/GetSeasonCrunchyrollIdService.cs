using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.GetSeasonCrunchyrollId;

public partial class GetSeasonCrunchyrollIdService : IGetSeasonCrunchyrollIdService
{
    private readonly IGetSeasonCrunchyrollIdRepository _repository;
    private readonly ILogger<GetSeasonCrunchyrollIdService> _logger;

    public GetSeasonCrunchyrollIdService(IGetSeasonCrunchyrollIdRepository repository, 
        ILogger<GetSeasonCrunchyrollIdService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<Result<CrunchyrollId?>> GetSeasonCrunchyrollId(CrunchyrollId seriesId, string seasonName, int? indexNumber, 
        CultureInfo language, CancellationToken cancellationToken)
    {
        //For example Attack on Titan has an "OAD" Season, then search by Folder Name
        var seasonIdResult = await GetSeasonIdByName(seriesId, seasonName, language, cancellationToken);

        if (seasonIdResult.IsFailed)
        {
            _logger.LogDebug("GetSeasonIdByName failed. Skipping season with name {Name}", seasonName);
            return seasonIdResult.ToResult();
        }

        if (seasonIdResult.Value is null)
        {
            if (!indexNumber.HasValue)
            {
                _logger.LogError("Item with name '{Name}' has no IndexNumber. Skipping...", seasonName);
                return Result.Fail(Domain.Constants.ErrorCodes.Internal);
            }

            seasonIdResult = await GetSeasonIdByNumber(seriesId, indexNumber!.Value,
                language, cancellationToken);
        }

        if (seasonIdResult.IsFailed)
        {
            _logger.LogDebug("GetSeasonIdByNumber failed. Skipping season with name {Name}", seasonName);
            return seasonIdResult.ToResult();
        }

        return seasonIdResult.Value;
    }
    
    private async Task<Result<CrunchyrollId?>> GetSeasonIdByName(CrunchyrollId seriesId, string name, 
        CultureInfo language, CancellationToken cancellationToken)
    {
        if (SeasonFolderNameHasOnlyNumberRegex().IsMatch(name))
        {
            return Result.Ok<CrunchyrollId?>(null);
        }
        
        return await _repository
            .GetSeasonIdByNameAsync(seriesId, name, language, cancellationToken);
    }
    
    private async Task<Result<CrunchyrollId?>> GetSeasonIdByNumber(CrunchyrollId seriesId, int? indexNumber, 
        CultureInfo language, CancellationToken cancellationToken)
    {
        var seasonIdResult = await _repository.GetSeasonIdByNumberAsync(seriesId, indexNumber!.Value,
            language, cancellationToken);

        if (seasonIdResult.IsFailed)
        {
            return seasonIdResult;
        }
        
        var seasonId = seasonIdResult.Value;

        if (!string.IsNullOrWhiteSpace(seasonId?.ToString()))
        {
            return seasonId;
        }

        var seasonsResult = await _repository.GetAllSeasonsAsync(seriesId, language,
            cancellationToken);

        if (seasonsResult.IsFailed)
        {
            return seasonsResult.ToResult();
        }

        foreach (var season in seasonsResult.Value)
        {
            var regex = new Regex($@"S{indexNumber}(DC)?$");
            var match = regex.Match(season.Identifier);

            if (match.Success)
            {
                return new CrunchyrollId(season.CrunchyrollId);
            }
        }

        return Result.Ok<CrunchyrollId?>(null);
    }
    
    [GeneratedRegex(@"^Season \d*$")]
    private static partial Regex SeasonFolderNameHasOnlyNumberRegex();
}