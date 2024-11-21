using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;

public interface IPostMovieScanTask
{
    public Task RunAsync(BaseItem item, CancellationToken cancellationToken);
}