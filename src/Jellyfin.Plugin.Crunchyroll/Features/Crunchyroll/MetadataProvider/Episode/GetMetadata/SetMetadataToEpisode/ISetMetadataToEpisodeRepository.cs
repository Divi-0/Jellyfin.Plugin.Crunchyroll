using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.SetMetadataToEpisode;

public interface ISetMetadataToEpisodeRepository
{
    public Task<Result<Domain.Entities.Episode?>> GetEpisodeAsync(CrunchyrollId episodeId, CultureInfo language,
        CancellationToken cancellationToken);
}