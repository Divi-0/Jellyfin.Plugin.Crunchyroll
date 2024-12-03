using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;

namespace Jellyfin.Plugin.Crunchyroll.Tests;

public static class MockHelper
{
    private static readonly object LibraryManackerLock = new object();
    private static readonly object ItemRepositoryLock = new object();
    private static readonly object MediaSourceManagerLock = new object();
    
    public static ILibraryManager LibraryManager
    {
        get
        {
            lock (LibraryManackerLock)
            {
                return BaseItem.LibraryManager ??= Substitute.For<ILibraryManager>();
            }
        }
    }

    public static IItemRepository ItemRepository
    {
        get
        {
            lock (ItemRepositoryLock)
            {
                return BaseItem.ItemRepository ??= Substitute.For<IItemRepository>();
            }
        }
    }

    public static IMediaSourceManager MediaSourceManager
    {
        get
        {
            lock (MediaSourceManagerLock)
            {
                return BaseItem.MediaSourceManager ??= Substitute.For<IMediaSourceManager>();
            }
        }
    }
}