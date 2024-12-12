using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetSpecialEpisodeCrunchyrollId;

public class GetSpecialEpisodeCrunchyrollIdService : IGetSpecialEpisodeCrunchyrollIdService
{
    private readonly IGetSpecialEpisodeCrunchyrollIdRepository _repository;

    public GetSpecialEpisodeCrunchyrollIdService(IGetSpecialEpisodeCrunchyrollIdRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<Result<CrunchyrollId?>> GetEpisodeIdAsync(CrunchyrollId seriesId, string fileName, CancellationToken cancellationToken)
    {
        var episodeIdResult = await _repository.GetEpisodeIdByNameAsync(seriesId, fileName, cancellationToken);

        if (episodeIdResult.IsFailed)
        {
            return episodeIdResult.ToResult();
        }
        
        return episodeIdResult.Value;
    }
}