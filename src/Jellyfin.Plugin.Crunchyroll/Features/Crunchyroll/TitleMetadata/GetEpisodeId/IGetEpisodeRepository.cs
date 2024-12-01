using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public interface IGetEpisodeRepository
{
    public Task<Result<EpisodeIdResult?>> GetEpisodeIdAsync(string crunchyrollSeasonId, string episodeIdentifier,
        CancellationToken cancellationToken);
    public Task<Result<EpisodeIdResult?>> GetEpisodeIdByNameAsync(string crunchyrollSeasonId, string episodeName,
        CancellationToken cancellationToken);
}