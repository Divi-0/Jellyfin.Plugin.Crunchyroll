using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using NSubstitute;

namespace Jellyfin.Plugin.ExternalComments.Tests.Shared.Faker
{
    public static class SeriesFaker
    {
        public static Series Generate()
        {
            var series = new Bogus.Faker<Series>()
                .RuleFor(x => x.Id, Guid.NewGuid())
                .RuleFor(x => x.Name, f => f.Random.Word())
                .Generate();

            var seasons = Enumerable
                .Range(1, 10)
                .Select(_ => SeasonFaker.Generate())
                .ToList<BaseItem>();

            BaseItem.ItemRepository
                .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                    x.ParentId == series.Id &&
                    x.GroupByPresentationUniqueKey == false &&
                    x.DtoOptions.Fields.Count != 0))
                .Returns(seasons);

            BaseItem.LibraryManager
                .GetItemById(series.Id)
                .Returns(series);

            return series;
        }

        public static Series GenerateWithTitleId()
        {
            var series = Generate();

            series.ProviderIds.Add(CrunchyrollExternalKeys.Id, CrunchyrollIdFaker.Generate());
            series.ProviderIds.Add(CrunchyrollExternalKeys.SlugTitle, CrunchyrollSlugFaker.Generate());

            return series;
        }
    }
}
