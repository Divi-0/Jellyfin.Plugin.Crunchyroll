using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.SetMetadataToMovie;

public interface ISetMetadataToMovieService
{
    public Task<Result<MediaBrowser.Controller.Entities.Movies.Movie>> SetMetadataToMovieAsync(CrunchyrollId seriesId,
        CrunchyrollId seasonId, CrunchyrollId episodeId, CultureInfo language, CancellationToken cancellationToken);
}