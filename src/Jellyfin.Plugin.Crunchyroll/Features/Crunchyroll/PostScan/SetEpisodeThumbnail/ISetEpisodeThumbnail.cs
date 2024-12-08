using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;

public interface ISetEpisodeThumbnail
{
    public Task<Result> GetAndSetThumbnailAsync(Episode episode, ImageSource imageSource, CancellationToken cancellationToken);
    public Task<Result> GetAndSetThumbnailAsync(Movie movie, ImageSource imageSource, CancellationToken cancellationToken);
}