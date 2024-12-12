using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteMovieJellyfinData;

public interface IOverwriteMovieJellyfinDataRepository
{
    public Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(string seriesId, 
        CultureInfo language, CancellationToken cancellationToken);
}