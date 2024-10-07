using MediaBrowser.Controller.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces
{
    public interface IPostEpisodeIdSetTask
    {
        public Task RunAsync(BaseItem episodeItem, CancellationToken cancellationToken);
    }
}
