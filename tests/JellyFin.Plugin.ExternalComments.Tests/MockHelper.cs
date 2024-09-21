using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;

namespace JellyFin.Plugin.ExternalComments.Tests;

public static class MockHelper
{
    private static readonly object LibraryManackerLock = new object();
    private static readonly object ItemRepositoryLock = new object();
    
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
}