using MediaBrowser.Controller.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces
{
    internal interface IPostSeasonIdSetTask
    {
        public Task RunAsync(BaseItem seasonItem, CancellationToken cancellationToken);
    }
}
