using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetEpisodeCrunchyrollId;

public partial class GetEpisodeCrunchyrollIdService : IGetEpisodeCrunchyrollIdService
{
    private readonly IGetEpisodeCrunchyrollIdRepository _repository;
    private readonly ILogger<GetEpisodeCrunchyrollIdService> _logger;

    public GetEpisodeCrunchyrollIdService(IGetEpisodeCrunchyrollIdRepository repository, 
        ILogger<GetEpisodeCrunchyrollIdService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<Result<CrunchyrollId?>> GetEpisodeIdAsync(CrunchyrollId seasonId, string fileName, int? indexNumber, CancellationToken cancellationToken)
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

        return episodeIdResult.Value;
    }
    
    [GeneratedRegex(@"E\d*\.\d*|E\d*[A-z]")]
    private static partial Regex IsDecimalNumberOrNumberWithLetterRegex();

    [GeneratedRegex(@"E-?([^ -]+)")]
    private static partial Regex EpisodeNameFormatRegex();
}