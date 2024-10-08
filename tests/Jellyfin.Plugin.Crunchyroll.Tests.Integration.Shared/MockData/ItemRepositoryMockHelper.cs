using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Persistence;
using NSubstitute;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;

public static class ItemRepositoryMockHelper
{
    public static List<Season> MockGetChildren(this IItemRepository itemRepository, Series parent, bool isSeasonIdSet = false)
    {
        var children = Enumerable.Range(1, Random.Shared.Next(1, 10))
            .Select(number =>
            {
                var season = isSeasonIdSet ? SeasonFaker.GenerateWithSeasonId(parent) : SeasonFaker.Generate(parent);
                season.IndexNumber = number;
                return season;
            })
            .ToList();
        
        itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x => x.ParentId == parent.Id))
            .Returns(children.ToList<BaseItem>());

        return children;
    }    
    
    public static List<Episode> MockGetChildren(this IItemRepository itemRepository, Season parent, bool isEpisodeIdSet = false)
    {
        var children = Enumerable.Range(1, Random.Shared.Next(1, 10))
            .Select(number =>
            {
                var episode = isEpisodeIdSet ? EpisodeFaker.GenerateWithEpisodeId(parent) : EpisodeFaker.Generate(parent);
                episode.IndexNumber = number;
                return episode;
            })
            .ToList();
        
        itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x => x.ParentId == parent.Id))
            .Returns(children.ToList<BaseItem>());

        return children;
    }
    
    public static void MockGetChildrenEmpty(this IItemRepository itemRepository, BaseItem parent)
    {
        itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x => x.ParentId == parent.Id))
            .Returns([]);
    }
}