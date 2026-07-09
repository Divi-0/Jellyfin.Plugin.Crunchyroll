using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetEpisodeCrunchyrollId;

public partial class GetEpisodeCrunchyrollIdService : IGetEpisodeCrunchyrollIdService
{
    private readonly IGetEpisodeCrunchyrollIdRepository _repository;
    private readonly ILogger<GetEpisodeCrunchyrollIdService> _logger;
    private readonly IScrapEpisodeMetadataService _scrapEpisodeMetadataService;

    public GetEpisodeCrunchyrollIdService(IGetEpisodeCrunchyrollIdRepository repository, 
        ILogger<GetEpisodeCrunchyrollIdService> logger,
        IScrapEpisodeMetadataService scrapEpisodeMetadataService)
    {
        _repository = repository;
        _logger = logger;
        _scrapEpisodeMetadataService = scrapEpisodeMetadataService;
    }
    
    public async Task<Result<CrunchyrollId?>> GetEpisodeIdAsync(CrunchyrollId seasonId, CrunchyrollId seriesId, 
        CultureInfo language, string fileName, int? indexNumber, CancellationToken cancellationToken)
    {
        string episodeIdentifier;

        if (IsDecimalNumberOrNumberWithLetterRegex().Match(fileName).Success)
        {
            indexNumber = null;
        }
        
        if (!indexNumber.HasValue)
        {
            var match = EpisodeNameFormatRegex().Match(fileName);

            if (!match.Success)
            {
                var episodeIdByNameResult = await _repository.GetEpisodeIdByName(seasonId, fileName, cancellationToken);

                if (episodeIdByNameResult.IsSuccess)
                {
                    return episodeIdByNameResult.Value;
                }
                
                _logger.LogDebug("Episode with name {Name} has no IndexNumber, number could not be read from name and id was " +
                                 "not found by episode name. Skipping...", 
                    fileName);
                return episodeIdByNameResult;
            }
            
            episodeIdentifier = match.Groups[1].Value.TrimStart('0');
        }
        else
        {
            episodeIdentifier = indexNumber.Value.ToString();
        }
        
        var episodeIdResult = await _repository.GetEpisodeIdByNumber(seasonId, episodeIdentifier, cancellationToken);

        if (episodeIdResult.IsFailed)
        {
            return episodeIdResult.ToResult();
        }

        if (episodeIdResult.Value is null)
        {
            var duplicateSeasonIdResult = await _repository
                .GetDuplicateNextSeasonIdAsync(seasonId, seriesId, cancellationToken);

            if (duplicateSeasonIdResult.IsFailed)
            {
                return duplicateSeasonIdResult;
            }

            var duplicateSeasonId = duplicateSeasonIdResult.Value;

            if (duplicateSeasonId is null)
            {
                _logger.LogDebug("Duplicate season not found for season Id {SeasonId}", seasonId);
                return (CrunchyrollId?)null;
            }

            _ = await _scrapEpisodeMetadataService.ScrapEpisodeMetadataAsync(duplicateSeasonId, null, language, cancellationToken);
            
            episodeIdResult =
                await _repository.GetEpisodeIdByNumberDuplicateNextSeasonAsync(seasonId, seriesId, episodeIdentifier,
                    cancellationToken);
        }

        return episodeIdResult;
    }
    
    //S01E10.5
    //S01E13A
    [GeneratedRegex(@"E\d*\.\d*|E\d*[A-z]")]
    private static partial Regex IsDecimalNumberOrNumberWithLetterRegex();

    // Match episode identifiers after an "E" (optionally followed by a hyphen).
    // Capture group 1 should contain the identifier. Acceptable forms:
    // - Numeric (e.g. 502, 1124)
    // - Decimal (e.g. 6.5, 32.9)
    // - Numeric with trailing letters (e.g. 13A, 52b)
    // - Short uppercase codes optionally followed by digits (e.g. SP, SP1, FMI1)
    // Do not match long words like "Harbors" so those will fall back to name-based lookup.
    // Match episode identifiers after an "E" (optionally followed by a hyphen).
    // Capture group 1 should contain the identifier. Acceptable forms:
    // - Numeric (e.g. 502, 1124)
    // - Decimal (e.g. 6.5, 32.9)
    // - Numeric with trailing letters (e.g. 13A, 52b)
    // - Short uppercase codes (2-3 letters) optionally followed by digits (e.g. SP, SP1, FMI1)
    // Ensure the identifier is a whole token (not the first letter of a longer lowercase word like "Harbors").
    [GeneratedRegex(@"E-?((?:[0-9]+(?:\.[0-9]+)?[A-Za-z]*)|(?:[A-Z]{2,3}[0-9]*))(?=$|[^A-Za-z0-9]|\b)")]
    private static partial Regex EpisodeNameFormatRegex();
}