using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode.GetEpisodeImageInfos;

public interface IGetEpisodeImageInfosRepository
{
    public Task<Result<Domain.Entities.Episode?>> GetEpisodeAsync(CrunchyrollId episodeId, 
        CancellationToken cancellationToken);
}