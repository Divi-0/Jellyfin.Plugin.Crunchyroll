using AutoFixture;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using NSubstitute;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;

public static class LibraryManagerMock
{
    public static List<BaseItem> MockCrunchyrollTitleIdScan(this ILibraryManager libraryManager)
    {
        var fixture = new Fixture();
        
        var itemList = fixture.Build<Series>()
            .CreateMany<Series>()
            .ToList<BaseItem>();
        
        libraryManager
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(itemList);
        
        libraryManager
            .GetItemById(Arg.Any<Guid>())
            .Returns(fixture.Create<Series>());
        
        libraryManager
            .UpdateItemAsync(Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return itemList;
    }
    
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
                    { CrunchyrollExternalKeys.Id, providerId }
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
}