using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId.Client;

public interface ICrunchyrollMovieEpisodeIdClient
{
    public Task<Result<SearchResponse?>> SearchTitleIdAsync(string name, CultureInfo language, CancellationToken cancellationToken);
}