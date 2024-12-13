using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId;

public interface IGetMovieCrunchyrollIdService
{
    public Task<Result<MovieCrunchyrollIdResult?>> GetCrunchyrollIdAsync(string fileName, 
        CultureInfo language, CancellationToken cancellationToken);
}