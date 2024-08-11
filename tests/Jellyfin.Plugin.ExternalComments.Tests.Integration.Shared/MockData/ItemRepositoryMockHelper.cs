using AutoFixture;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Persistence;
using NSubstitute;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;

public static class ItemRepositoryMockHelper
{
    public static List<Season> MockGetChildren(this IItemRepository itemRepository, BaseItem item)
    {
        var fixture = new Fixture();

        var children = fixture.CreateMany<Season>().ToList();
        
        itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x => x.ParentId == item.Id))
            .Returns(children.ToList<BaseItem>());

        return children;
    }
}