using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.ScrapMovieMetadata;

public interface IScrapMovieMetadataService
{
    public Task<Result> ScrapMovieMetadataAsync(CrunchyrollId seriesId, CrunchyrollId seasonId, CrunchyrollId episodeId, CultureInfo language,
        CancellationToken cancellationToken);
}