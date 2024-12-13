using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie.GetMovieImageInfos;

public interface IGetMovieImageInfosRepository
{
    public Task<Result<ImageSource?>> GetEpisodeThumbnailAsync(CrunchyrollId episodeId, 
        CancellationToken cancellationToken);
}