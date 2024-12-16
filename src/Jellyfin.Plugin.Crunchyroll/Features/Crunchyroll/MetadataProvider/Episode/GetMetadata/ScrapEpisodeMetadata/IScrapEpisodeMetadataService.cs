using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;

public interface IScrapEpisodeMetadataService
{
    public Task<Result> ScrapEpisodeMetadataAsync(CrunchyrollId seasonId, CrunchyrollId? episodeId, CultureInfo language, 
        CancellationToken cancellationToken);
}