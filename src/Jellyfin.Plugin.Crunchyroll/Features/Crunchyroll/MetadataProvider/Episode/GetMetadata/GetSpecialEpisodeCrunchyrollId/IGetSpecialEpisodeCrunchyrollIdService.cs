using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetSpecialEpisodeCrunchyrollId;

public interface IGetSpecialEpisodeCrunchyrollIdService
{
    public Task<Result<CrunchyrollId?>> GetEpisodeIdAsync(CrunchyrollId seriesId, string fileName, CancellationToken cancellationToken);
}