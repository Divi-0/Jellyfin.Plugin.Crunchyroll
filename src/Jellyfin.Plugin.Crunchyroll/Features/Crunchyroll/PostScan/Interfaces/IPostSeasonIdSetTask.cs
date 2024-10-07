using MediaBrowser.Controller.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces
{
    public interface IPostSeasonIdSetTask
    {
        public Task RunAsync(BaseItem seasonItem, CancellationToken cancellationToken);
    }
}
