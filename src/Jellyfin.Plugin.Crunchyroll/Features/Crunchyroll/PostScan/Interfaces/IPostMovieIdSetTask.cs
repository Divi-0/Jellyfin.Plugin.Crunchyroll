using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;

public interface IPostMovieIdSetTask
{
    public Task RunAsync(Movie movie, CancellationToken cancellationToken);
}