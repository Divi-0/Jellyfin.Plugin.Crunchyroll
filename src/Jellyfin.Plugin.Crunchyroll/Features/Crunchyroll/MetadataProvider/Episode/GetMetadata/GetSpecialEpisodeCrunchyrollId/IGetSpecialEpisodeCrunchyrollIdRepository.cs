using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetSpecialEpisodeCrunchyrollId;

public interface IGetSpecialEpisodeCrunchyrollIdRepository
{
    public Task<Result<CrunchyrollId?>> GetEpisodeIdByNameAsync(CrunchyrollId seriesId, string name, 
        CancellationToken cancellationToken);
}