using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetEpisodeCrunchyrollId;

public interface IGetEpisodeCrunchyrollIdService
{
    public Task<Result<CrunchyrollId?>> GetEpisodeIdAsync(CrunchyrollId seasonId, string fileName, int? indexNumber,
        CancellationToken cancellationToken);
}