using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.ScrapMissingEpisode;

public interface IScrapMissingEpisodeService
{
    public Task<Result> ScrapMissingEpisodeAsync(CrunchyrollId episodeId, CultureInfo language,
        CancellationToken cancellationToken);
}