using MediaBrowser.Controller.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;

public interface IPostSeriesScanTask
{
    public Task RunAsync(BaseItem item, CancellationToken cancellationToken);
}

