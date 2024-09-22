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
