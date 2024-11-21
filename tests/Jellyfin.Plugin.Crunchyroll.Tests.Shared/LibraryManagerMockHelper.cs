using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using NSubstitute;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared;

public static class LibraryManagerMockHelper
{
    public static ILibraryManager MockGetItemsResultById(this ILibraryManager libraryManager, string id, string? providerId = null!)
    {
        BaseItem.LibraryManager = libraryManager;
        
        var parentId = Guid.NewGuid();

        if (providerId is null)
        {
            providerId = Guid.NewGuid().ToString();
        }
        
        libraryManager
            .GetItemById(parentId)
            .Returns(new Series()
            {
                ProviderIds = new Dictionary<string, string>
                {
                    { CrunchyrollExternalKeys.SeriesId, providerId }
                }
            });

        libraryManager
            .GetItemsResult(Arg.Is<InternalItemsQuery>(x => x.AncestorWithPresentationUniqueKey == id))
            .Returns(new MediaBrowser.Model.Querying.QueryResult<BaseItem>()
            {
                Items = new List<BaseItem>()
                {
                    new Series()
                    {
                        ParentId = parentId
                    }
                }
            });

        return libraryManager;
    }
    
    public static ILibraryManager MockGetItemsResultByIdEmptyResult(this ILibraryManager libraryManager, string id)
    {
        libraryManager
            .GetItemsResult(Arg.Is<InternalItemsQuery>(x => x.AncestorWithPresentationUniqueKey == id))
            .Returns(new MediaBrowser.Model.Querying.QueryResult<BaseItem>());

        return libraryManager;
    }
    
    public static ILibraryManager MockRetrieveItem(this ILibraryManager libraryManager, Guid id, string? providerId = null!)
    {
        if (providerId is null)
        {
            providerId = Guid.NewGuid().ToString();
        }
        
        libraryManager
            .RetrieveItem(id)
            .Returns(new Series()
            {
                ProviderIds = new Dictionary<string, string>
                {
                    { CrunchyrollExternalKeys.SeriesId, providerId }
                }
            });

        return libraryManager;
    }
    
    public static ILibraryManager MockRetrieveItemNotFound(this ILibraryManager libraryManager, Guid id)
    {
        libraryManager
            .RetrieveItem(id)
            .Returns((BaseItem)null!);

        return libraryManager;
    }
}