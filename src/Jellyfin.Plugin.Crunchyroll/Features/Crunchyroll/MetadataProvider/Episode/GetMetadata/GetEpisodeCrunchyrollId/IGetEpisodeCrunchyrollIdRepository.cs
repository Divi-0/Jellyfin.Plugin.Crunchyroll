using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetEpisodeCrunchyrollId;

public interface IGetEpisodeCrunchyrollIdRepository
{
    public Task<Result<CrunchyrollId?>> GetEpisodeIdByName(CrunchyrollId seasonId, string name,
        CancellationToken cancellationToken);
    
    public Task<Result<CrunchyrollId?>> GetEpisodeIdByNumber(CrunchyrollId seasonId, string episodeNumber,
        CancellationToken cancellationToken);
}