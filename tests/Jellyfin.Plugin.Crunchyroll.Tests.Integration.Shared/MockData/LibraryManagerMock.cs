using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using NSubstitute;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;

public static class LibraryManagerMock
{
    public static List<Series> MockCrunchyrollTitleIdScan(this ILibraryManager libraryManager,
        IItemRepository itemRepository, string libraryName, List<Series>? items = null)
    {
        var itemList = items?.ToList() ?? Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => SeriesFaker.Generate())
            .ToList();
        
        var folderList = new List<BaseItem>();
        foreach (var series in itemList)
        {
            var folder = new Folder
            {
                Id = Guid.NewGuid()
            };

            libraryManager
                .GetItemById(folder.Id)
                .Returns(folder);
            
            itemRepository
                .GetItemList(Arg.Is<InternalItemsQuery>(x => x.ParentId == folder.Id))
                .Returns([series]);
            
            folderList.Add(folder);
        }

        var collectionFolder = new CollectionFolder()
        {
            Id = Guid.NewGuid(),
            PhysicalFolderIds = folderList.Select(x => x.Id).ToArray()
        };
        libraryManager
            .GetItemList(Arg.Is<InternalItemsQuery>(x => x.Name == libraryName))
            .Returns([collectionFolder]);

        itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x => x.ParentId == collectionFolder.Id))
            .Returns(itemList.ToList<BaseItem>());

        foreach (var item in itemList)
        {
            libraryManager
                .GetItemById(item.Id)
                .Returns(item);
            
            libraryManager
                .UpdateItemAsync(item, item.DisplayParent, Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
        }

        return itemList;
    }
    
    public static List<Movie> MockCrunchyrollTitleIdScanMovies(this ILibraryManager libraryManager,
        IItemRepository itemRepository, string libraryName, List<Movie>? items = null)
    {
        var itemList = items?.ToList() ?? Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => MovieFaker.Generate())
            .ToList();

        var folderList = new List<BaseItem>();
        foreach (var movie in itemList)
        {
            var folder = new Folder
            {
                Id = Guid.NewGuid()
            };

            libraryManager
                .GetItemById(folder.Id)
                .Returns(folder);
            
            itemRepository
                .GetItemList(Arg.Is<InternalItemsQuery>(x => x.ParentId == folder.Id))
                .Returns([movie]);
            
            folderList.Add(folder);
        }

        var collectionFolder = new CollectionFolder()
        {
            Id = Guid.NewGuid(),
            PhysicalFolderIds = folderList.Select(x => x.Id).ToArray()
        };
        
        libraryManager
            .GetItemList(Arg.Is<InternalItemsQuery>(x => x.Name == libraryName))
            .Returns([collectionFolder]);

        foreach (var item in itemList)
        {
            libraryManager
                .GetItemById(item.Id)
                .Returns(item);
            
            libraryManager
                .UpdateItemAsync(item, item.DisplayParent, Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
        }

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
}