using MediaBrowser.Controller.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces
{
    public interface IPostScanTask
    {
        public Task RunAsync(BaseItem item, CancellationToken cancellationToken);
    }
}
