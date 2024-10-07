using MediaBrowser.Controller.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces
{
    public interface IPostTitleIdSetTask
    {
        public Task RunAsync(BaseItem seriesItem, CancellationToken cancellationToken);
    }
}
