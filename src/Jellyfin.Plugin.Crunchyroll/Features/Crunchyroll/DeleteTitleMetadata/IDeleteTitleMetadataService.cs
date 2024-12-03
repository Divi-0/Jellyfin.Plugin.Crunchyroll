using System.Globalization;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.DeleteTitleMetadata;

public interface IDeleteTitleMetadataService
{
    public Task DeleteTitleMetadataAsync(BaseItem baseItem);
}