using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.DeleteTitleMetadata;

public class DeleteTitleMetadataService : IDeleteTitleMetadataService
{
    private readonly IDeleteTitleMetadataRepository _repository;

    public DeleteTitleMetadataService(IDeleteTitleMetadataRepository repository)
    {
        _repository = repository;
    }
    
    public async Task DeleteTitleMetadataAsync(BaseItem baseItem)
    {
        await GetChildrenAndDeleteRecursiveAsync([baseItem]);

        _ = await _repository.SaveChangesAsync(CancellationToken.None);
    }
    
    private async Task GetChildrenAndDeleteRecursiveAsync(IEnumerable<BaseItem> items)
    {
        foreach (var item in items)
        {
            if (item is Folder folder)
            {
                await GetChildrenAndDeleteRecursiveAsync(folder.Children);
            }
                    
            if (item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var seriesId) &&
                !string.IsNullOrWhiteSpace(seriesId))
            {
                _ = await _repository.DeleteTitleMetadataAsync(seriesId, item.GetPreferredMetadataCultureInfo());
            }
        }
    }
}