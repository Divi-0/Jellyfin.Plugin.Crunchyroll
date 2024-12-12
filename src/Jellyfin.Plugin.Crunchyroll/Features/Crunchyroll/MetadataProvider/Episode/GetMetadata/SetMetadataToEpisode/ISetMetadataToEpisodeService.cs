using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.SetMetadataToEpisode;

public interface ISetMetadataToEpisodeService
{
    public Task<Result<MediaBrowser.Controller.Entities.TV.Episode>> SetMetadataToEpisodeAsync(CrunchyrollId episodeId,
        int? currentIndexNumber, int? parentIndexNumber, CultureInfo language, CancellationToken cancellationToken);
}