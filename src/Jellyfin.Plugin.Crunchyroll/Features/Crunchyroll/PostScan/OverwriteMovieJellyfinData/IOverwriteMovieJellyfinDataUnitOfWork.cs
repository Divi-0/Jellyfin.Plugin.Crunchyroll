using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteMovieJellyfinData;

public interface IOverwriteMovieJellyfinDataUnitOfWork
{
    public ValueTask<Result<TitleMetadata.Entities.TitleMetadata?>> GetTitleMetadataAsync(string seriesId, CancellationToken cancellationToken);
}